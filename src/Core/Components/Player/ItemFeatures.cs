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
using SDG.Unturned;

namespace Essentials.Core.Components.Player
{
    internal class ItemFeatures : PlayerComponent
    {
        private readonly PlayerEquipment     _equip;

        public bool AutoRepair { get; set; }
        public bool AutoReload { get; set; }

        public ItemFeatures()
        {
            _equip = Player.Equipment;
        }

        private void FixedUpdate()
        {
            if ( _equip.HoldingItemID == 0 ) return;

            /*
                Weapon feature (Auto reload)
            */
            if ( AutoReload && _equip.state.Length >= 10 && _equip.state[0xA] < 5 )
            {
                var id = BitConverter.ToUInt16( 
                    new[] { _equip.state[0x8], _equip.state[0x9] }, 
                    0x0 
                );

                var maga = Assets.find( EAssetType.ITEM, id ) as ItemMagazineAsset;

                _equip.state[0xA] = maga?.amount ?? 50;
                _equip.sendUpdateState();
            }
            
            /*
                Item feature (Auto repair)
            */
            if ( AutoRepair && _equip.quality < 90 )
            {
                _equip.quality = 100;
                _equip.sendUpdateQuality();
            }
        }
    }
}
