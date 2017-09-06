﻿using Assets.Src.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Src.Rocket
{
    class RocketRunner : IRocketRunner
    {
        //public int DetonateWithLessThanXRemainingFuel = -100;
        private ITargetDetector _targetDetector;
        private ITargetPicker _targetPicker;
        private IPilot _pilot;
        private readonly RocketController _rocketController;
        private string _previousTarget;
        private IDetonator _detonator;

        public RocketRunner(ITargetDetector targetDetector, ITargetPicker targetPicker, IPilot engineControl, IDetonator detonator)
        {
            _targetDetector = targetDetector;
            _targetPicker = targetPicker;
            _pilot = engineControl;
            _detonator = detonator;
        }

        public void RunRocket()
        {
            var allTargets = _targetDetector.DetectTargets();
            var bestTarget = _targetPicker.FilterTargets(allTargets).OrderByDescending(t => t.Score).FirstOrDefault();

            if (bestTarget != null)
            {
                _pilot.Fly(bestTarget);
            }

            if (_pilot.StartDelay <= 0)
            {
                _detonator.AutoDetonate(bestTarget);
            }

            //Disabled detonation when out of fuel.
            //if(_engineControl.RemainingFuel < DetonateWithLessThanXRemainingFuel)
            //{
            //    _detonator.DetonateNow();
            //}
        }
    }
}
