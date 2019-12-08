﻿using Assets.Src.Targeting;
using UnityEngine;

namespace Assets.Src.Interfaces
{
    public interface ITarget
    {
        string Team { get; set; }
        Transform Transform { get; }
        Rigidbody Rigidbody { get; }
        ShipType Type { get; }

        /// <summary>
        /// True if ships should manuvre towards or away from this (or go round and round it if they want to).
        /// </summary>
        bool NavigationalTarget { get; }

        /// <summary>
        /// True of turrets and missiles should try to kill this if it's an enemy.
        /// </summary>
        bool AtackTarget { get; }
    }
}
