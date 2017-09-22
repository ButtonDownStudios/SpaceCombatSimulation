﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO neaten up fields and methods.
public class EngineControler : MonoBehaviour {
    /// <summary>
    /// old version of engineFoce - retained to copy values
    /// </summary>
    public Vector3 EngineForce;

    /// <summary>
    /// The force the engine applys at this transform's position in this transfornm's -up direction
    /// </summary>
    public float EngineForce2;

    public Transform Pilot;
    public FuelTank FuelTank;
    public Rigidbody ForceApplier;
    public ParticleSystem Plume;

    public float TranslateFireAngle = 45;
    public float TorqueFireAngle = 90;

    public bool UseAsTorquer = true;
    public bool UseAsTranslator = true;

    public float FullThrottleFuelConsumption = 1;

    /// <summary>
    /// The world space vector the engine should try to fly towards.
    /// Use null or zero for no force
    /// </summary>
    public Vector3? FlightVector;
    
    /// <summary>
    /// Calculated if not set (default)
    /// </summary>
    public Vector3? TorqueVector = null;

    /// <summary>
    /// throttle for torquing will be set to angle to turn / TorquerFullThrottleAngle capped at 1.
    /// </summary>
    public float TorquerFullThrottleAngle = 10;

    private bool _active = true;
    private string InactiveTag = "Untagged";

    public float _fullTrhrottlePlumeRate;

    // Use this for initialization
    void Start () { 
        if(Plume != null)
        {
            _fullTrhrottlePlumeRate = Plume.emission.rateOverTime.constant;
        }
        if(EngineForce2 <= 0)
        {
            Debug.Log(transform.name + " needs it's engine force set correctly.");
            EngineForce2 = EngineForce.magnitude;
        }

        Pilot = FindOtherComponents(transform);
        
        if(Pilot != transform)
        {
            NotifyPilot();
        }
        if (FuelTank == null)
        {
            Debug.LogWarning(transform.name + " found no fuel tank - INFINITE FUEL!");
        }
        if(ForceApplier == null)
        {
            Debug.LogError("Engine found no rigidbody to apply forces to");
        }
        CalculateEngineTorqueVector();
    }
    
    // Update is called once per frame
    void Update () {
        //Debug.Log(transform + ":");
        if (_active && HasFuel())
        {
            float throttle = 0;

            if (UseAsTranslator)
            {
                throttle = TranslateThrotleSetting();
            }

            if (UseAsTorquer && throttle < 1 && ApplysCorrectTorque())
            {
                var angle = Vector3.Angle(Pilot.forward, FlightVector.Value);
                var additionalThrottle = angle / TorquerFullThrottleAngle;
                throttle = Math.Min(throttle + additionalThrottle, 1);
            }

            if(throttle > 0)
            {
                throttle = AdjustThrottleForFuel(throttle);

                ForceApplier.AddForceAtPosition(-transform.up * EngineForce2 * throttle, transform.position);
                //ForceApplier.AddRelativeForce(EngineForce * throttle);
                SetPlumeState(throttle);
                return;
            }
        }
        SetPlumeState(0);
    }

    private bool HasFuel()
    {
        if(FuelTank != null)
        {
            return FuelTank.Fuel > 0;
        }
        return true;
    }

    private float AdjustThrottleForFuel(float throttle)
    {
        if(FuelTank != null)
        {
            var fuel = FuelTank.DrainFuel(throttle * FullThrottleFuelConsumption);
            throttle = fuel * FullThrottleFuelConsumption;
        }
        return throttle;
    }

    private void SetPlumeState(float throttle)
    {
        if (throttle > 0)
        {
            //Debug.Log("turning plume on");
            Plume.Play();

            //reduce rate for throttle.
            var emission = Plume.emission;
            var rate = emission.rateOverTime;
            rate.constant = _fullTrhrottlePlumeRate * throttle;
            emission.rateOverTime = rate;
        } else
        {
            //Debug.Log("turning plume off");
            Plume.Stop();
        }
    }

    public void Deactivate()
    {
        //Debug.Log("Deactivating " + name);
        _active = false;
        SetPlumeState(0);
        tag = InactiveTag;
    }

    private bool ApplysCorrectTorque()
    {
        if (FlightVector.HasValue && FlightVector.Value.magnitude > 0 && TorqueVector.HasValue && TorqueVector.Value.magnitude > 0.5)
        {
            var pilotSpaceVector = Pilot.InverseTransformVector(FlightVector.Value);

            var rotationVector = new Vector3(-pilotSpaceVector.y, pilotSpaceVector.x, 0);   //set z to 0 to not add spin

            var angle = Vector3.Angle(TorqueVector.Value, rotationVector);

            //Debug.Log("torquer to vector angle: " + angle);
            //Debug.Log(_torqueVector + " - " + FlightVector.Value);
            return angle < TorqueFireAngle;
        }
        //Debug.Log("vectors not set");
        return false;
    }

    private float TranslateThrotleSetting()
    {
        if(FlightVector.HasValue && FlightVector.Value.magnitude > 0)
        {
            //the enemy's gate is down
            var angle = Vector3.Angle(-transform.up, FlightVector.Value);
            //Debug.Log("fire angle = " + angle);

            var throttle = Math.Max(0, 1 - (angle / TranslateFireAngle));
            //Debug.Log("TranslateThrotleSetting=" + throttle);
            return throttle;
        }
        //Debug.Log("No FlightVector set Defaulting To False");
        return 0;
    }

    private Transform FindOtherComponents(Transform transform)
    {
        if(Pilot != null && FuelTank != null && ForceApplier != null)
        {
            //everyhting's set already, so stop looking.
            return Pilot;
        }
        if (FuelTank == null)
        {
            //first object found with a fuel tank
            FuelTank = transform.GetComponent("FuelTank") as FuelTank;
        }
        if(ForceApplier == null)
        {
            //firstComponent with a rigidbody
            ForceApplier = transform.GetComponent("Rigidbody") as Rigidbody;
        }
        var parent = transform.parent;
        if (parent == null && Pilot == null)
        {
            //pilot is highest in hierarchy
            Pilot = transform;
            return transform;
        }
        return FindOtherComponents(parent);
    }

    private void NotifyPilot()
    {
        //Debug.Log("Registering engine with " + parent);
        Pilot.SendMessage("RegisterEngine", this, SendMessageOptions.DontRequireReceiver);
    }

    private Vector3? CalculateEngineTorqueVector()
    {
        if (!TorqueVector.HasValue)
        {
            var pilotSpaceVector = Pilot.InverseTransformVector(-transform.up);
            var pilotSpaceEngineLocation = Pilot.InverseTransformPoint(transform.position);
            var xTorque = (pilotSpaceEngineLocation.y * pilotSpaceVector.z) - (pilotSpaceEngineLocation.z * pilotSpaceVector.y);
            var yTorque = (pilotSpaceEngineLocation.x * pilotSpaceVector.z) + (pilotSpaceEngineLocation.z * pilotSpaceVector.x);
            var zTorque = (pilotSpaceEngineLocation.y * pilotSpaceVector.x) + (pilotSpaceEngineLocation.x * pilotSpaceVector.y);
            TorqueVector = new Vector3(xTorque, yTorque, zTorque);
        }
        return TorqueVector;
    }
}
