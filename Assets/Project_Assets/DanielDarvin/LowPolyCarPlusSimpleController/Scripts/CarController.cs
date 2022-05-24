using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public enum Axel
{
    Front,
    Rear
}

[Serializable]
public struct Wheel
{
    public GameObject model;
    public WheelCollider collider;
    public Axel axel;
}

public class CarController : MonoBehaviour
{
    [Header("Hotkeys")]
    [Tooltip("Selects a hotkey for nitro action.")]
    [SerializeField]
    private KeyCode m_nitroKey = KeyCode.LeftControl;
    [Tooltip("Selects a hotkey for brake action.")]
    [SerializeField]
    private KeyCode m_brakeKey = KeyCode.Space;
    [SerializeField]
    private KeyCode m_lightKey = KeyCode.E;

    [Header("EasySuspension")]
    [Range(0, 20)]
    public float naturalFrequency = 10;
    [Range(0, 3)]
    public float dampingRatio = 0.8f;
    [Range(-1, 1)]
    public float forceShift = 0.03f;
    [Tooltip("Adjust the length of the suspension springs according to the natural frequency and damping ratio. When off, can cause unrealistic suspension bounce.")]
    public bool setSuspensionDistance = true;

    [Header("Car Parts")]
    [SerializeField]
    private List<Wheel> m_wheels;
    [SerializeField]
    private TrailRenderer[] m_tireMarks;
    [SerializeField]
    private TrailRenderer[] m_lightLine;
    [SerializeField]
    private ParticleSystem[] m_tireSmoke;
    [SerializeField]
    private ParticleSystem[] m_tireDust;
    [SerializeField]
    private bool m_enablemDust = true;
    [SerializeField]
    private bool m_enablemTireMarks = true;

    [Header("Car Properties")]
    public float KPH = 0f;
    [Tooltip("Choose your vehicle's center of gravity.")]
    [SerializeField]
    private Vector3 m_centerOfMass;
    [Tooltip("The number of seconds for nitro to take effect.")]
    [SerializeField]
    private float m_nitrusValue = 10f;
    [Tooltip("Sensitivity when turning the vehicle.")]
    [SerializeField]
    private float m_maxAcceleration = 20.0f;
    [SerializeField]
    private float m_turnSensitivity = 1.0f;
    [SerializeField]
    private float m_brakeTorque = 50f;
    [SerializeField]
    private float m_maxSteerAngle = 45.0f;

    [Tooltip("The vehicle's speed when the physics engine can use different amount of sub-steps (in m/s).")]
    public float criticalSpeed = 5f;
    [Tooltip("Simulation sub-steps when the speed is above critical.")]
    public int stepsBelow = 5;
    [Tooltip("Simulation sub-steps when the speed is below critical.")]
    public int stepsAbove = 1;
    [SerializeField]
    private AnimationCurve m_brakeLine;

    [Header("Light Properties")]
    [SerializeField]
    private Material m_lightMaterial;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color m_lightColor;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color m_lightColorPress;
    [SerializeField]
    private Material m_stopLightMaterial;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color m_stopLightColor;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color m_stopLightColorPress;
    private WheelFrictionCurve m_forwardFriction, m_sidewaysFriction;
    private float m_inputX, m_inputY;
    private Rigidbody m_rigidbody;
    private bool m_brake = false;
    private float m_dragTime = 6.0f;
    private bool m_boosting = false;
    private bool m_light = false;
    public PhotonView view;

    public float GetSpeed()
    {
        return KPH;
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, 0.2f);
    }

    public void SetActivDust(bool state)
    {
        m_enablemDust = state;
    }

    public void SetActiveTireMarks(bool state)
    {
        m_enablemTireMarks = state;
    }

    private void OnEnable()
    {
        foreach (var wheel in m_wheels)
        {
            wheel.collider.ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);
        }
    }

    private void Start()
    {
        if (view.IsMine)
        {
            m_rigidbody = GetComponent<Rigidbody>();
            m_rigidbody.centerOfMass = m_centerOfMass;
            m_forwardFriction = m_wheels[0].collider.forwardFriction;
        }
    }

    private void Update()
    {
        if (view.IsMine)
        {
            AnimateWheels();
            GetInputs();
        }
    }

    private void LateUpdate()
    {
        if (view.IsMine)
        {
            Move();
            Turn();
            ActivateNitrus();
            ActivateLight();
            if (m_enablemDust)
            {
                if (IsGrounded())
                {
                    DustEffect(true);
                }
                else
                {
                    DustEffect(false);
                }
            }
            else
            {
                DustEffect(false);
            }
        }
    }

    private void GetInputs()
    {
        m_inputX = Input.GetAxis("Horizontal");
        m_inputY = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(m_brakeKey))
        {
            m_brake = true;
        }
        if (Input.GetKeyUp(m_brakeKey))
        {
            m_brake = false;
        }
        if (Input.GetKeyDown(m_nitroKey))
        {
            m_boosting = true;
        }
        if (Input.GetKeyUp(m_nitroKey))
        {
            m_boosting = false;
        }
        if (Input.GetKeyDown(m_lightKey))
        {
            m_light = true;
        }
        if (Input.GetKeyUp(m_lightKey))
        {
            m_light = false;
        }
    }

    private void AnimateWheels()
    {
        foreach (var wheel in m_wheels)
        {
            Quaternion _rot;
            Vector3 _pos;
            wheel.collider.GetWorldPose(out _pos, out _rot);
            wheel.model.transform.position = _pos;
            wheel.model.transform.rotation = _rot;
        }
    }

    private void Move()
    {
        foreach (var wheel in m_wheels)
        {
            if (m_brake)
            {
                EnableBrake(wheel.collider);
                TireMarksEffect(true);
            }
            else
            {
                DisableBrake(wheel.collider);
                TireMarksEffect(false);
            }
            if (m_brake == false)
            {
                wheel.collider.motorTorque = m_inputY * m_maxAcceleration;
            }
        }
        foreach (var wheel in m_wheels)
        {
            if (wheel.collider.isGrounded)
            {
                KPH = m_rigidbody.velocity.magnitude * 3.6f;
                //SmokeEffect(true);
                EasySuspension(wheel.collider);
            }
            else
            {
                TireMarksEffect(false);
            }
        }
    }

    private void EnableBrake(WheelCollider wheel)
    {
        m_dragTime -= Time.deltaTime;
        m_brakeLine.Evaluate(Mathf.Clamp(m_dragTime, 0f, 2f));
        float driftSmothFactor = 1 * m_brakeLine.Evaluate(Time.deltaTime);
        m_forwardFriction.stiffness = Mathf.Lerp(m_forwardFriction.stiffness, 6, driftSmothFactor);
        m_stopLightMaterial.SetColor("_EmissionColor", m_stopLightColorPress);
        m_stopLightMaterial.SetFloat("_EmissiveExposureWeight", 0.2f);
        wheel.brakeTorque = m_brakeTorque * KPH;
        wheel.forwardFriction = m_forwardFriction;
    }

    private void DisableBrake(WheelCollider wheel)
    {
        m_dragTime = 2.0f;
        m_forwardFriction.stiffness = 1f;
        wheel.brakeTorque = 0f;
        wheel.forwardFriction = m_forwardFriction;
        m_stopLightMaterial.SetColor("_EmissionColor", m_stopLightColor);
        m_stopLightMaterial.SetFloat("_EmissiveExposureWeight", 0.8f);
    }

    private void TireMarksEffect(bool state)
    {
        if (m_enablemTireMarks)
        {
            if (m_tireMarks[0].emitting != state)
            {
                foreach (TrailRenderer tireMark in m_tireMarks)
                {
                    tireMark.emitting = state;
                }
            }
            SmokeEffect(state);
        }
    }

    private void SmokeEffect(bool state)
    {
        if (state)
        {
            if (m_tireSmoke[0].isPaused)
            {
                foreach (var smoke in m_tireSmoke)
                {
                    smoke.Play();
                }
            }
        }
        else
        {
            if (m_tireSmoke[0].isPlaying)
            {
                foreach (var smoke in m_tireSmoke)
                {

                    smoke.Pause();
                }
            }
        }
    }

    private void DustEffect(bool state)
    {
        float rate = state ? 8f : 0f;
        if (m_tireDust[0].emission.rateOverDistance.constant != rate)
        {
            foreach (var dust in m_tireDust)
            {
                var emission = dust.emission;
                emission.rateOverDistance = rate;
            }
        }
    }

    private void EasySuspension(WheelCollider wheel)
    {
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = Mathf.Pow(Mathf.Sqrt(wheel.sprungMass) * naturalFrequency, 2);
        spring.damper = 2 * dampingRatio * Mathf.Sqrt(spring.spring * wheel.sprungMass);
        wheel.suspensionSpring = spring;
        Vector3 wheelRelativeBody = transform.InverseTransformPoint(wheel.transform.position);
        float distance = GetComponent<Rigidbody>().centerOfMass.y - wheelRelativeBody.y + wheel.radius;
        wheel.forceAppPointDistance = distance - forceShift;
        if (spring.targetPosition > 0 && setSuspensionDistance)
        {
            wheel.suspensionDistance = wheel.sprungMass * Physics.gravity.magnitude / (spring.targetPosition * spring.spring);
        }
    }

    private void Turn()
    {
        foreach (var wheel in m_wheels)
        {
            if (wheel.axel == Axel.Front)
            {
                var _steerAngel = m_inputX * m_turnSensitivity * m_maxSteerAngle;
                wheel.collider.steerAngle = Mathf.Lerp(wheel.collider.steerAngle, _steerAngel, 0.5f);
            }
        }
    }

    private void ActivateLight()
    {
        if (m_light)
        {
            m_lightMaterial.SetColor("_EmissionColor", m_lightColorPress);
            m_lightMaterial.SetFloat("_EmissiveExposureWeight", 0.2f);
        }
        else
        {
            m_lightMaterial.SetColor("_EmissionColor", m_lightColor);
            m_lightMaterial.SetFloat("_EmissiveExposureWeight", 0.8f);
        }
    }

    public void ActivateNitrus()
    {
        if (!m_boosting && m_nitrusValue <= 10)
        {
            m_nitrusValue += Time.deltaTime / 2;
        }
        else
        {
            m_nitrusValue -= (m_nitrusValue <= 0) ? 0 : Time.deltaTime;
        }
        if (m_boosting)
        {
            if (m_nitrusValue > 0)
            {
                m_rigidbody.AddForce(transform.forward * 5000);
                if (m_lightLine[0].emitting == false)
                {
                    foreach (var lightLine in m_lightLine)
                    {
                        lightLine.emitting = true;
                    }
                }
            }
            else
            {
                if (m_lightLine[0].emitting)
                {
                    foreach (var lightLine in m_lightLine)
                    {
                        lightLine.emitting = false;
                    }
                }
            }
        }
        else
        {
            if (m_lightLine[0].emitting)
            {
                foreach (var lightLine in m_lightLine)
                {
                    lightLine.emitting = false;
                }
            }
        }
    }
}
