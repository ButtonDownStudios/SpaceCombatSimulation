﻿using Assets.Src.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Src.Evolution;
using System.Linq;

namespace Assets.Src.ModuleSystem
{
    public class ModuleTypeKnower : MonoBehaviour, IGeneticConfigurable
    {
        [Tooltip("the list of types that this module can act as.")]
        public List<ModuleType> Types;

        [Tooltip("the cost for this module when evolving ships.")]
        public float Cost = 100;

        //[Tooltip("These components will be configured in order by this behaviour when Configure is called on it.")]
        //public List<IGeneticConfigurable> ComponentsToConfigure = new List<IGeneticConfigurable>();
        
        public GenomeWrapper Configure(GenomeWrapper genomeWrapper)
        {
            var ComponentsToConfigure = GetComponents<IGeneticConfigurable>();
            if (ComponentsToConfigure.Length > 1)   //if length == 1 then this has only found itself.
            {
                foreach (var c in ComponentsToConfigure)
                {
                    if (c != this)
                    {
                        genomeWrapper = c.Configure(genomeWrapper);
                    }
                }
            }
            return genomeWrapper;
        }
    }
}