using UnityEngine;

namespace SteeringBehaviours
{
    [RequireComponent(typeof(Rigidbody))]
    public class SteeringAgent : MonoBehaviour
    {
        [Header("Settings")]
        public SteeringModeType m_SteeringModeType = SteeringModeType.Seek;

        [SerializeField]
        private float m_MaxForce = 1f;
        public void SetMaxForce(float maxForce) => m_MaxForce = maxForce;

        [SerializeField]
        private float m_MaxSpeed = 1f;
        public void SetMaxSpeed(float maxSpeed) => m_MaxSpeed = maxSpeed;

        [SerializeField]
        public float m_PredictionFactor = 1f;
        public void SetPredictionFactor(float predicitonFactor) => m_PredictionFactor = predicitonFactor;

        [SerializeField]
        private bool m_ArrivingAdjustments = false;

        [SerializeField]
        private float m_SlowingDistance = 4f;

        private Rigidbody m_Rigidbody;
        public Rigidbody Rigidbody {  get { return m_Rigidbody; } }
        
        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public void UpdatePhysics()
        {
            var targetPostion = GameManager.Instance.Target.position;
            var steeringDirection =  GetSteeringDirection(m_Rigidbody.position, m_Rigidbody.velocity, 
                targetPostion, targetPostion + GameManager.Instance.Target.velocity * m_PredictionFactor);

            Debug.DrawRay(targetPostion + GameManager.Instance.Target.velocity * m_PredictionFactor, Vector3.up, Color.red);

            var velocity = CalculateVelocity(steeringDirection, m_Rigidbody.velocity, m_Rigidbody.mass);
            if(velocity.magnitude > Mathf.Epsilon)
                m_Rigidbody.MoveRotation(Quaternion.LookRotation(velocity));
            m_Rigidbody.velocity = velocity;
        }

        public Vector3 GetSteeringDirection(Vector3 currentPosition, Vector3 currentVelocity, Vector3 targetPosition, Vector3 targetPredictedPosition)
        {
            Vector3 desiredVelocity = Vector3.zero;

            switch (m_SteeringModeType)
            {
                case SteeringModeType.Seek:
                    if(m_ArrivingAdjustments)
                    {
                        var targetOffset = targetPosition - currentPosition;
                        var distance = targetOffset.magnitude;
                        var rampedSpeed = m_MaxSpeed * (distance / m_SlowingDistance);
                        var clippedSpeed = Mathf.Min(rampedSpeed, m_MaxSpeed);
                        desiredVelocity = (clippedSpeed / distance) * targetOffset;
                    }
                    else
                        desiredVelocity = (targetPosition - currentPosition).normalized * m_MaxSpeed;
                    break;
                case SteeringModeType.Flee:
                    desiredVelocity = (currentPosition - targetPosition).normalized * m_MaxSpeed;
                    break;
                case SteeringModeType.Pursuit:
                    if(m_ArrivingAdjustments)
                    {
                        var targetOffset = targetPredictedPosition - currentPosition;
                        var distance = targetOffset.magnitude;
                        var rampedSpeed = m_MaxSpeed * (distance / m_SlowingDistance);
                        var clippedSpeed = Mathf.Min(rampedSpeed, m_MaxSpeed);
                        desiredVelocity = (clippedSpeed / distance) * targetOffset;
                    }
                    else
                    desiredVelocity = (targetPredictedPosition - currentPosition).normalized * m_MaxSpeed;
                    break;
                case SteeringModeType.Evasion:
                    desiredVelocity = (currentPosition - targetPredictedPosition).normalized * m_MaxSpeed;
                    break;
            }

            return desiredVelocity - currentVelocity;
        }

        public Vector3 CalculateVelocity(Vector3 steeringDirection, Vector3 currentVelocity, float mass)
        {
            var steeringForce = Vector3.ClampMagnitude(steeringDirection, m_MaxForce);
            var acceleration = steeringForce / mass;
            return Vector3.ClampMagnitude(currentVelocity + acceleration, m_MaxSpeed);
        }
    }
}
