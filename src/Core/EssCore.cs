/*
 *  This file is part of uEssentials project.
 *      https://uessentials.github.io/
 *
 *  Copyright (C) 2015-2016  Leonardosc
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Essentials.Api;
using Essentials.Api.Event;
using Essentials.Api.Logging;
using Essentials.Api.Module;
using Essentials.Api.Task;
using Essentials.Api.Unturned;
using Essentials.Commands;
using Essentials.Common;
using Essentials.Configuration;
using Essentials.Core.Command;
using Essentials.Core.Event;
using Essentials.I18n;
using Essentials.Kits;
using Essentials.Warps;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Plugins;
using Environment = Rocket.Core.Environment;
using Essentials.Common.Reflect;
using Essentials.Updater;
using Rocket.API.Serialisation;
using Rocket.Core.Permissions;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace Essentials.Core
{
    public sealed class EssCore : RocketPlugin
    {
       /*
            TODO:
                - AFK KICK
            
            Aliases
                - /tptome -> /tp $1 sender_name
                - /expme 20 -> /exp give $1 sender_name

            PERMISSIONS:
                - essentials.kit.<kit_name>
                - essentials.kit.<kit_name>.other // allow to give kit to another player
                - essentials.kits.bypasscooldown
                - essentials.warp.<warp_name>
                - essentials.warps.bypasscooldown
                - essentials.back
                - essentials.spy
                - essentials.antispam.bypass
                - essentials.command.poll.start
                - essentials.command.poll.stop
                - essentials.command.poll.list
                - essentials.command.poll.info
                - essentials.keepskill.<skill>
        */
        
        internal const string                         PLUGIN_VERSION              = "1.0.7.0";
        internal const string                         ROCKET_VERSION              = "4.9.2.0";
        internal const string                         UNTURNED_VERSION            = "3.14.4.1";
        
        internal static EssCore                       Instance                    { get; set; }
        
        internal WarpManager                          WarpManager                 { get; set; }
        internal KitManager                           KitManager                  { get; set; }
        internal ModuleManager                        ModuleManager               { get; set; }
        internal CommandManager                       CommandManager              { get; set; }
        internal IEventManager                        EventManager                { get; set; }
        internal IUpdater                             Updater                     { get; set; }

        internal EssConfig                            Config                      { get; set; }
        internal EssLogger                            Logger                      { get; set; }

        internal HashSet<UPlayer>                     ConnectedPlayers            { get; set; }

        private string _folder;
        private string _translationFolder;
        private string _dataFolder;
        private string _modulesFolder;

        internal string Folder
        {
            get
            {
                if ( !System.IO.Directory.Exists( _folder ) )
                {
                    System.IO.Directory.CreateDirectory( _folder );
                }
                return _folder;
            }
        }

        internal string TranslationFolder
        {
            get
            {
                if ( !System.IO.Directory.Exists( _translationFolder ) )
                {
                    System.IO.Directory.CreateDirectory( _translationFolder );
                }
                return _translationFolder;
            }
        }

        internal string DataFolder
        {
            get
            {
                if ( !System.IO.Directory.Exists( _dataFolder ) )
                {
                    System.IO.Directory.CreateDirectory( _dataFolder );
                }
                return _dataFolder;
            }
        }

        internal string ModulesFolder
        {
            get
            {
                if ( !System.IO.Directory.Exists( _modulesFolder ) )
                {
                    System.IO.Directory.CreateDirectory( _modulesFolder );
                }
                return _modulesFolder;
            }
        }

        protected override void Load()
        {
            Instance = this;

            R.Permissions = new EssentialsPermissionsProvider();

            Provider.onServerDisconnected += PlayerDisconnectCallback;
            Provider.onServerConnected += PlayerConnectCallback;

            Logger = new EssLogger( "[uEssentials] " );
            ConnectedPlayers = new HashSet<UPlayer>();

            _folder = Environment.PluginsDirectory + "/uEssentials/";
            _translationFolder = Folder + "translations/";
            _dataFolder = Folder + "data/";
            _modulesFolder = Folder + "modules/";

            (
                from directory in new[] { _folder, _translationFolder, _dataFolder, _modulesFolder }
                where !System.IO.Directory.Exists( directory )
                select directory
            ).ForEach( dir => System.IO.Directory.CreateDirectory( dir ) );

            var configPath = $"{Folder}config.json";

            Config = new EssConfig();
            Config.Load( configPath );

            EventManager = new EventManager();
            CommandManager = new CommandManager();
            WarpManager = new WarpManager();
            KitManager = new KitManager();
            ModuleManager = new ModuleManager();
            Updater = new GithubUpdater();

            var configFiels = typeof (EssConfig).GetFields();
            var hasNulls = configFiels.Any( f => f.GetValue( Config ) == null );
            var nonNullValues = new Dictionary<string, object>();

            /*
                If has any null value it will update the config
            */
            if ( hasNulls )
            {
                /*
                    Save nonnull values
                */
                configFiels.Where( f => f.GetValue( Config ) != null ).ForEach( f =>
                {
                    nonNullValues.Add( f.Name, f.GetValue( Config ) );
                } );

                /*
                    Reload defaults
                */
                Config.LoadDefaults();

                /*
                    Restore nonnull values
                */
                nonNullValues.ForEach( pair =>
                {
                    typeof(EssConfig).GetField( pair.Key ).SetValue( Config, pair.Value );
                } );

                Config.Save( configPath );
            }

            EssLang.Load();

            Logger.Log( "Loaded uEssentials", ConsoleColor.Green );
            Logger.Log( "Plugin version: ", ConsoleColor.Green, suffix: "" );
            Logger.Log( PLUGIN_VERSION, ConsoleColor.White, "" );
            Logger.Log( "Recommended Rocket version: ", ConsoleColor.Green, suffix: "" );
            Logger.Log( ROCKET_VERSION, ConsoleColor.White, "" );
            Logger.Log( "Recommended Unturned version: ", ConsoleColor.Green, suffix: "" );
            Logger.Log( UNTURNED_VERSION, ConsoleColor.White, "" );
            Logger.Log( "Author: ", ConsoleColor.Green, suffix: "" );
            Logger.Log( "leonardosc", ConsoleColor.White, "" );
            Logger.Log( "Wiki: ", ConsoleColor.Green, suffix: "" );
            Logger.Log( "uessentials.github.io", ConsoleColor.White, "" );

            EventManager.RegisterAll( GetType().Assembly );
            CommandManager.RegisterAll( GetType().Assembly );
            WarpManager.Load();
            KitManager.Load();

            Logger.LogInfo( $"Loaded {CommandManager.Commands.Count()} commands" );
            Logger.LogInfo( $"Loaded {WarpManager.Count} warps" );
            Logger.LogInfo( $"Loaded {KitManager.Count} kits" );

            Logger.LogInfo( "Loading modules..." );
            ModuleManager.LoadAll( ModulesFolder );
            Logger.LogInfo( $"Loaded {ModuleManager.RunningModules.Count} modules" );

            if ( Config.AutoAnnouncer.Enabled )
            {
                Config.AutoAnnouncer.Start();
            }

            if ( Config.DisabledCommands.Count != 0 )
            {
                Config.DisabledCommands.ForEach( cmdName =>
                {
                    var command = CommandManager.GetByName( cmdName );

                    if ( command == null || command is CommandEssentials )
                    {
                        Logger.LogWarning( $"There no command named '{cmdName}' to unregister." );
                    }
                    else
                    {
                        CommandManager.Unregister( command );
                        Logger.LogInfo( $"Unregistered command: '{cmdName}'" );   
                    }
                } );
            }

            if ( CommandManager.HasWithName( "tp" ) )
            {
                UnregisterRocketCommand<Rocket.Unturned.Commands.CommandTp>();
            }

            if ( CommandManager.HasWithName( "item" ) )
            {
                UnregisterRocketCommand<Rocket.Unturned.Commands.CommandI>();
            }

            #if DEV
            CommandWindow.ConsoleOutput.title = "Unturned Server";
            #endif

            #if !DEV
            if ( Config.Updater.CheckUpdates )
            {
                new System.Threading.Thread( () =>
                {
                    Logger.LogInfo( "Checking updates." );

                    var isUpdated = Updater.IsUpdated();
                    var lastResult = Updater.LastResult;

                    if ( isUpdated )
                    {
                        Logger.LogInfo( "Plugin is up-to-date!" );
                        return;
                    }

                    Logger.LogInfo( $"New version avalaible: {lastResult.LatestVersion}" );

                    if ( !lastResult.AdditionalData.IsNullOrEmpty() )
                    {
                        Newtonsoft.Json.Linq.JToken changesStr;
                        if ( Newtonsoft.Json.Linq.JObject.Parse( lastResult.AdditionalData ).TryGetValue( "changes", out changesStr ) )
                        {
                            Logger.LogInfo( "========================= [ Changes ] =========================" );

                            changesStr.ToString().Split( '\n' ).ForEach( msg =>
                            {
                                Logger.Log( "", ConsoleColor.Green, suffix: "" );
                                Logger.Log( "  " + msg, ConsoleColor.White, "" );
                            } );

                            Logger.LogInfo( "" );
                            Logger.Log( "", ConsoleColor.Green, suffix: "" );
                            Logger.Log( "  " +  $"See more in: https://github.com/uEssentials/uEssentials/releases/tag/{lastResult.LatestVersion}", ConsoleColor.White, "" );

                            Logger.LogInfo( "===============================================================" );
                        }
                    }

                    if ( Config.Updater.DownloadLatest )
                    {
                        Updater.DownloadLatestRelease( $"{Folder}/updates/" );
                    }
                } ).Start();
            }

            Analytics.Metrics.Init();
            #endif

            TryAddComponent<Tasks>();

            Tasks.New( t => {
                WarpManager.Save();
            }).Delay( 60 * 1000 ).Interval( 60 * 1000 ).Go();

            Tasks.New( t => {
                File.Delete( $"{Folder}uEssentials.en.translation.xml" );
                File.Delete( $"{Folder}uEssentials.configuration.xml" );
            } ).Delay( 100 ).Go();

            CommandWindow.ConsoleInput.onInputText += ReloadCallback;
        }

        protected override void Unload()
        {
            CommandWindow.ConsoleInput.onInputText -= ReloadCallback;
            Provider.onServerDisconnected -= PlayerDisconnectCallback;
            Provider.onServerConnected -= PlayerConnectCallback;

            WarpManager.Save();
            var executingAssembly = GetType().Assembly;
            
            CommandManager.UnregisterAll( executingAssembly );
            EventManager.UnregisterAll( executingAssembly ); 
            ModuleManager.UnloadAll();

            Tasks.CancelAll();
        }

        private static void UnregisterRocketCommand<T>() where T : IRocketCommand
        {
            var rocketCommands = AccessorFactory.AccessField<List<IRocketCommand>>( R.Commands, "commands" );

            rocketCommands.Value.RemoveAll( cmd => cmd is T );
        }

        private static void ReloadCallback( string command )
        {
            if ( !command.StartsWith( "rocket reload", true, CultureInfo.InvariantCulture ) ) return;

            Console.WriteLine();
            EssProvider.Logger.LogError( "Rocket reload cause many issues, consider restart the server" );
            EssProvider.Logger.LogError( "Or use '/essentials reload' to reload essentials correctly." );
            Console.WriteLine();
        }

        private static void PlayerConnectCallback( CSteamID id )
        {
            Instance.ConnectedPlayers.Add( new UPlayer( UnturnedPlayer.FromCSteamID( id ) ) );
        }

        private static void PlayerDisconnectCallback( CSteamID id )
        {
            Instance.ConnectedPlayers.RemoveWhere( connectedPlayer => connectedPlayer.CSteamId == id );
        }

        private class EssentialsPermissionsProvider : IRocketPermissionsProvider
        {
            private readonly IRocketPermissionsProvider _defaultProvider;

            public EssentialsPermissionsProvider()
            {
                _defaultProvider = R.Instance.GetComponent<RocketPermissionsManager>();
            }

            public bool HasPermission( IRocketPlayer player, string requestedPermission, bool defaultReturnValue = false )
            {
                return _defaultProvider.HasPermission( player, requestedPermission, defaultReturnValue );
            }

            public bool HasPermission( IRocketPlayer player, string requestedPermission, out uint? cooldownLeft, bool defaultReturnValue = false )
            {
                var essCommand = Instance.CommandManager.GetByName( requestedPermission );

                if ( essCommand != null )
                {
                    return _defaultProvider.HasPermission( player, essCommand.Permission, out cooldownLeft, defaultReturnValue );
                }

                if ( !Instance.CommandManager.HasWithName( requestedPermission ) )
                {
                    cooldownLeft = 0;
                    return true;
                }

                return _defaultProvider.HasPermission( player, requestedPermission, out cooldownLeft, defaultReturnValue );
            }

            public List<RocketPermissionsGroup> GetGroups( IRocketPlayer player, bool includeParentGroups )
            {
                return _defaultProvider.GetGroups( player, includeParentGroups );
            }

            public List<Permission> GetPermissions( IRocketPlayer player )
            {
                return _defaultProvider.GetPermissions( player );
            }

            public bool SetGroup( IRocketPlayer player, string groupID )
            {
                return _defaultProvider.SetGroup( player, groupID );
            }

            public void Reload()
            {
                _defaultProvider.Reload();
            }
        }
    }
}
