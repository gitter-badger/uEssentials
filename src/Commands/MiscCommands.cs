﻿/*
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
using Essentials.Api.Command;
using Essentials.Api.Command.Source;
using Essentials.Api.Unturned;
using Essentials.Common;
using Essentials.Core.Command;
using Essentials.Core.Components.Player;
using Essentials.I18n;
using Rocket.Unturned.Items;
using SDG.Unturned;
using UnityEngine;

namespace Essentials.Commands
{
    public class MiscCommands
    {
        private static readonly ICommandArgument One = new CommandArgument( 0, "1" );
        public static readonly List<string> Spies = new List<string>();

        [CommandInfo(
            Name = "ascend",
            Aliases = new []{"asc"},
            Usage = "[amount]",
            Description = "Ascend X \"meters\".",
            AllowedSource = AllowedSource.PLAYER
        )]
        public void AscendCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( args.IsEmpty )
            {
                ShowUsage( src, cmd );
            }
            else if ( !args[0].IsFloat )
            {
                EssLang.INVALID_NUMBER.SendTo( src, args[0] );
            }
            else if ( args[0].ToFloat <= 0 )
            {
                EssLang.MUST_POSITIVE.SendTo( src, args[0] );
            }
            else
            {
                var player = src.ToPlayer();
                var pos = new Vector3(player.Position.x, player.Position.y, player.Position.z);
                var num = args[0].ToFloat;

                pos.y += num;

                player.Teleport( pos );
                player.SendMessage( $"You ascended {num} \"meters\"" );
            }
        }

        [CommandInfo(
            Name = "descend",
            Aliases = new[] {"desc"},
            Usage = "[amount]",
            Description = "Descend X \"meters\".",
            AllowedSource = AllowedSource.PLAYER
        )]
        public void DescendCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( args.IsEmpty )
            {
                ShowUsage( src, cmd );
            }
            else if ( !args[0].IsFloat )
            {
                EssLang.INVALID_NUMBER.SendTo( src, args[0] );
            }
            else if ( args[0].ToFloat <= 0 )
            {
                EssLang.MUST_POSITIVE.SendTo( src );
            }
            else
            {
                var player = src.ToPlayer();
                var pos = new Vector3( player.Position.x, player.Position.y, player.Position.z );
                var num = args[0].ToFloat;

                pos.y -= num;

                player.Teleport( pos );
                player.SendMessage( $"You descended {num} \"meters\"" );
            }
        }

        [CommandInfo(
            Name = "clear",
            Description = "Clear things",
            Usage = "-i = items, -v = vehicles"
        )]
        public void ClearCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( args.IsEmpty )
            {
                ShowUsage( src, cmd );
                return;
            }

            var joinedArgs = args.Join( 0 );

            Func<string, bool> hasArg = arg =>
            {
                return joinedArgs.IndexOf( $"-{arg}", 0, StringComparison.InvariantCultureIgnoreCase ) != -1 ||
                        (joinedArgs.Contains( "-a" ) || joinedArgs.Contains( "-A" ));
            };

            /*
                TODO: Options
                    -i = items
                    -v = vehicles
                    -z = zombies
                    -b = barricades
                    -s = structures
                    -a = ALL
                
                /clear -i -z -v = items, zombies, vehicles
            */

            if ( hasArg( "i" ) )
            {
                ItemManager.askClearAllItems();
                EssLang.CLEAR_ITEMS.SendTo( src );
            }

            if ( hasArg( "v" ) )
            {
                UWorld.Vehicles.ForEach( v => {
                    for ( byte i = 0; i < v.passengers.Length; i++ )
                    {
                        v.kickPlayer( i );
                    }
                } );

                VehicleManager.askVehicleDestroyAll();
                EssLang.CLEAR_VEHICLES.SendTo( src );
            }
        }

        [CommandInfo(
            Name = "item",
            Usage = "[item] <amount> or [player|all] [item] [amount]",
            Aliases = new []{ "i" }
        )]
        public void ItemCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            switch (args.Length)
            {
                /*
                    /i [item]
                 */
                case 1:
                    if ( src.IsConsole )
                    {
                        goto usage;
                    }
                    GiveItem( src, src.ToPlayer(), args[0], One );
                    return;
                
                /*
                    /i [item] [amount]
                    /i [player] [item]
                    /i all [item]
                 */
                case 2:
                    if ( args[1].IsInt )
                    {
                        if ( src.IsConsole )
                        {
                            goto usage;
                        }
                        GiveItem( src, src.ToPlayer(), args[0], args[1] );
                    }
                    else if ( args[0].Is( "all" ) )
                    {
                        GiveItem( src, null, args[1], One, true );
                    }
                    else if ( !args[0].IsValidPlayerName )
                    {
                        EssLang.PLAYER_NOT_FOUND.SendTo( src, args[0] );
                    }
                    else
                    {
                        GiveItem( src, UPlayer.From( args[0].ToString() ), args[1], One );
                    }
                    return;
                
                /*
                    /i [player] [item] [amount]
                    /i all [item] [amount]
                 */
                case 3:
                    if ( args[0].Is( "all" ) )
                    {
                        GiveItem( src, null, args[1], args[2], true );   
                    }
                    else if ( !args[0].IsValidPlayerName )
                    {
                        EssLang.PLAYER_NOT_FOUND.SendTo( src, args[0] );
                    }
                    else
                    {
                        GiveItem( src, UPlayer.From( args[0].ToString() ), args[1], args[2] );
                    }
                    return;

                default:
                    goto usage;
            }

            usage:
            ShowUsage( src, cmd );
        }

        [CommandInfo(
            Name = "iteminfo",
            Aliases = new [] {"ii"},
            Description = "See informations about an item.",
            Usage = "<item_id>"
        )]
        public void ItemInfoCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( src.IsConsole && args.Length != 1 )
            {
                ShowUsage( src, cmd );
                return;
            }

            ItemAsset asset;

            if ( args.Length == 0 )
            {
                var equipament = src.ToPlayer().Equipment;

                if ( equipament.HoldingItemID == 0 )
                {
                    EssLang.EMPTY_HANDS.SendTo( src );
                }

                asset = equipament.asset;
            }
            else
            {
                if ( !args[0].IsUshort ||
                    (asset = Assets.find( EAssetType.ITEM, args[0].ToUshort ) as ItemAsset) == null)
                {
                    EssLang.INVALID_ITEM_ID.SendTo( src, args[0] );
                    return;
                }
            }

            var color       = Color.yellow;
            var name        = WrapMessage( src, asset.name );
            var description = WrapMessage( src, asset.Description );
            var type        = WrapMessage( src, asset.ItemType.ToString() );
            var isPro       = WrapMessage( src, asset.isPro.ToString() );


            src.SendMessage( $"Name: {name}", color );
            src.SendMessage( $"Description: {description}", color );
            src.SendMessage( $"Id: {asset.id}", color );
            src.SendMessage( $"Type: {type}", color );
            src.SendMessage( $"IsPro: {isPro}", color );
        }

        [CommandInfo(
            Name = "itemfeatures",
            Aliases = new []{ "if" },
            Usage = "[autoreload | autorepair] [on|off]",
            Description = "Item features",
            AllowedSource = AllowedSource.PLAYER
        )]
        public void ItemFeaturesCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( args.Length != 2 )
            {
                goto usage;
            }

            bool toggleValue;

            if ( args[1].IsOneOf( new[] { "1", "on", "true" } ) )
            {
                toggleValue = true;
            }
            else if ( args[1].IsOneOf( new[] { "0", "off", "false" } ) )
            {
                toggleValue = false;
            }
            else
            {
                goto usage;
            }

            var player = src.ToPlayer();
            var component = player.GetComponent<ItemFeatures>() ?? player.AddComponent<ItemFeatures>();

            switch (args[0].ToLowerString)
            {
                case "autoreload":
                    if ( toggleValue )
                    {
                        component.AutoReload = true;
                        EssLang.AUTO_RELOAD_ENABLED.SendTo( src );
                    }
                    else
                    {
                        component.AutoReload = false;
                        EssLang.AUTO_RELOAD_DISABLED.SendTo( src );
                    }
                    return;

                case "autorepair":
                    if ( toggleValue )
                    {
                        component.AutoRepair = true;
                        EssLang.AUTO_REPAIR_ENABLED.SendTo( src );
                    }
                    else
                    {
                        component.AutoRepair = false;
                        EssLang.AUTO_REPAIR_DISABLED.SendTo( src );
                    }
                    return;
                
                default:
                    goto usage;
            }
            
            usage:
            ShowUsage( src, cmd );
        }

        [CommandInfo(
            Name = "vehiclefeatures",
            Aliases = new []{ "vehfeatures", "vf" },
            Usage = "[autorefuel | autorepair] [on|off]",
            Description = "Vehicle features",
            AllowedSource = AllowedSource.PLAYER
        )]
        public void VehicleFeaturesCommand( ICommandSource src, ICommandArgs args, ICommand cmd )
        {
            if ( args.Length != 2 )
            {
                goto usage;
            }

            bool toggleValue;

            if ( args[1].IsOneOf( new[] { "1", "on", "true" } ) )
            {
                toggleValue = true;
            }
            else if ( args[1].IsOneOf( new[] { "0", "off", "false" } ) )
            {
                toggleValue = false;
            }
            else
            {
                goto usage;
            }
            
            var player = src.ToPlayer();
            var component = player.GetComponent<PlayerVehicleFeatures>() ?? player.AddComponent<PlayerVehicleFeatures>();

            switch (args[0].ToLowerString)
            {
                case "autorefuel":
                    if ( toggleValue )
                    {
                        component.AutoRefuel = true;
                        EssLang.AUTO_REFUEL_ENABLED.SendTo( src );
                    }
                    else
                    {
                        component.AutoRefuel = false;
                        EssLang.AUTO_REFUEL_DISABLED.SendTo( src );
                    }
                    return;

                case "autorepair":
                    if ( toggleValue )
                    {
                        component.AutoRepair = true;
                        EssLang.AUTO_REPAIR_ENABLED.SendTo( src );
                    }
                    else
                    {
                        component.AutoRepair = false;
                        EssLang.AUTO_REPAIR_DISABLED.SendTo( src );
                    }
                    return;
                
                default:
                    goto usage;
            }
            
            usage:
            ShowUsage( src, cmd );
        }

        [CommandInfo(
            Name = "spy",
            Description = "Toggle spy mode",
            AllowedSource = AllowedSource.PLAYER
        )]
        public void SpyCommand( ICommandSource source, ICommandArgs parameters )
        {
            var displayName = source.DisplayName;

            if ( Spies.Contains( displayName ) )
            {
                Spies.Remove( displayName );
                EssLang.SPY_MODE_OFF.SendTo( source );
            }
            else
            {
                Spies.Add( displayName );
                EssLang.SPY_MODE_ON.SendTo( source );
            }
        }

        # region HELPER METHODS

        private static void ShowUsage(ICommandSource src, ICommand cmd)
        {
            src.SendMessage( $"Use /{cmd.Name} {cmd.Usage}" );
        }

        private static string WrapMessage( ICommandSource src, string str )
        {
            if ( str == null )
                return "null";

            if ( str.Length < 90 || src.IsConsole )
		        return str;
		
	        return str.Substring(0, 90 - 3) + "...";
        }

        private static void GiveItem( ICommandSource src, UPlayer target, ICommandArgument itemArg, 
                                      ICommandArgument amountArg, bool allPlayers = false )
        {
            ItemAsset asset;

            if ( itemArg.IsUshort )
            {
                var id = itemArg.ToUshort;
                asset = (ItemAsset) Assets.find( EAssetType.ITEM, id );
            }
            else
            {
                asset = UnturnedItems.GetItemAssetByName( itemArg.ToLowerString );
            }

            if ( asset == null )
            {
                src.SendMessage( $"Could not find an item with this name or id: {itemArg}" );
                return;
            }

            ushort amt = 1;

            if ( amountArg != null )
            {
                if ( !amountArg.IsShort )
                {
                    EssLang.INVALID_NUMBER.SendTo( src, amountArg );
                }
                else if ( amountArg.ToUshort <= 0 )
                {
                    EssLang.MUST_POSITIVE.SendTo( src );
                }
                else
                {
                    amt = amountArg.ToUshort;
                    goto give;
                }
                return;
            }

            give:
            var playersToReceive = new List<UPlayer>();
            var item = new Item( asset.id, true );

            if ( allPlayers )
            {
                UServer.Players.ForEach( playersToReceive.Add );
                EssLang.GIVEN_ITEM_ALL.SendTo( src, amt, asset.Name );
            }
            else
            {
                playersToReceive.Add( target );

                if ( !src.IsConsole && src.ToPlayer() != target )
                {
                    EssLang.GIVEN_ITEM.SendTo( src, amt, asset.Name, target.CharacterName );
                }
            }

            playersToReceive.ForEach( p =>
            {
                var success = p.GiveItem( item, amt, true );

                EssLang.RECEIVED_ITEM.SendTo( p, amt, asset.Name );

                if ( !success )
                {
                    EssLang.INVENTORY_FULL.SendTo( p );
                }
            } );
        }

        #endregion
    }
}
