using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (CapsuleCollider))]
    public class RigidbodyFPCCulled : MonoBehaviour
    {
        public GameObject FacingDirection;
        public Camera Cam;
        public Rigidbody m_RigidBody;
        public float TargetGroundSpeed = 2;
        public float AccelerationFactor = 1;
        public float DecelerationFraction = 0.05f;
        public float AirControl = 0.3f;
        public float KickSpeed = 50f;
        public float TurnSpeed = 0.4f;
        public MouseLook mouseLook = new MouseLook();

        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_IsGrounded, m_CameraBackward;


        public Vector3 Velocity
        {
            get { return m_RigidBody.velocity; }
        }

        public bool Grounded
        {
            get { return m_IsGrounded; }
        }


        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init (transform, Cam.transform);
        }

        #region Update Functions

        private void Update()
        {
            RotateView();
            ActionInputs();
        }
        private void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation (transform, FacingDirection.transform);
            
        }
        private void ActionInputs()
        {
            if(Input.GetButtonDown("Fire1"))
            {
                ApplyKick(KickSpeed);
            }

            if(m_IsGrounded && Input.GetButtonDown("Jump"))
            {
                m_RigidBody.velocity += m_GroundContactNormal * 20;
            }

            m_CameraBackward = Input.GetButton("TurnAround");
        } 
        void ApplyKick(float Speed)
        {
            Vector3 kickVel = -1 * Cam.transform.forward * Speed;

            m_RigidBody.velocity += kickVel;
        }

        #endregion

        #region FixedUpdate functions

        private void FixedUpdate()
        {
            GroundCheck();
            MovementInput();
            TurnAroundUpdate();
        }
        private void GroundCheck()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
                                   ((m_Capsule.height / 2f) - m_Capsule.radius) + 0.1f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }
        }
        private void MovementInput()
        {
            Vector3 accelDir;
            Vector3 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            accelDir = FacingDirection.transform.forward * input.y + FacingDirection.transform.right * input.x;
            accelDir = Vector3.ProjectOnPlane(accelDir, m_GroundContactNormal).normalized;

            if (m_IsGrounded)
            {
                m_RigidBody.velocity += accelDir * AccelerationFactor;


                if (m_RigidBody.velocity.magnitude > TargetGroundSpeed)
                    m_RigidBody.velocity *= 0.95f;
                if(input == Vector3.zero)
                {
                    Vector3 reducedVel = m_RigidBody.velocity - m_RigidBody.velocity.normalized * TargetGroundSpeed * DecelerationFraction;
                    m_RigidBody.velocity = Vector3.Dot(reducedVel, m_RigidBody.velocity) < 0 ? Vector3.zero : reducedVel;
                }
            }
            else
            {
                m_RigidBody.velocity += accelDir * AccelerationFactor * AirControl;
            }
        }
        private void TurnAroundUpdate()
        {
            Quaternion target = Quaternion.identity;
            if (m_CameraBackward)
                target = Quaternion.Euler(0, 180, 0);
            Cam.transform.localRotation = Quaternion.Lerp(Cam.transform.localRotation, target, TurnSpeed);
        }
        #endregion


    }
}
