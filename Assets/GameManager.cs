using UnityEngine;

namespace SteeringBehaviours
{
    public class GameManager : MonoBehaviour
    {
        public Rigidbody m_Target;
        public Rigidbody Target {  get { return m_Target; } }

        [SerializeField]
        private float m_ScrollbarMultiplier = 10f;

        public float m_TargetSpeed = 10f;

        private Vector3 m_TargetPosition;

        private static GameManager instance;
        public static GameManager Instance { get { return instance; } }

        [Header("Debug")]
        [SerializeField]
        private bool m_EnableDebug = true;

        [SerializeField]
        private float m_TimeDiff = 0.3f;

        [SerializeField]
        private Color m_AgentColor = Color.green;

        [SerializeField]
        private Color m_TargetColor = Color.blue;

        [SerializeField]
        private float m_DebugSize = 0.1f;

        [SerializeField]
        private int m_MaxDebugPoints = 30;

        [SerializeField]
        private UnityEngine.UI.Text m_DebugText;

        private float m_PredictionDist = 0.1f;

        private SteeringAgent[] agents;

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("There can be only one GameManager in the scene!");
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Start()
        {
            agents = GameObject.FindObjectsOfType<SteeringAgent>();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void SetSteeringMode(string newMode)
        {
            m_DebugText.text = "Mode: " + newMode;
            SteeringModeType steeringModeType;
            if (System.Enum.TryParse(newMode, out steeringModeType))
            {
                foreach (var steeringAgent in GameObject.FindObjectsOfType<SteeringAgent>())
                {
                    steeringAgent.m_SteeringModeType = steeringModeType;
                }
            }
        }

        public void SetMaxForce(float maxForce)
        {
            foreach (var steeringAgent in GameObject.FindObjectsOfType<SteeringAgent>())
            {
                steeringAgent.SetMaxForce(maxForce * m_ScrollbarMultiplier);
            }
        }

        public void SetMaxSpeed(float maxSpeed)
        {
            foreach (var steeringAgent in GameObject.FindObjectsOfType<SteeringAgent>())
            {
                steeringAgent.SetMaxSpeed(maxSpeed * m_ScrollbarMultiplier);
            }
        }

        public void SetPredictionFactor(float prediction)
        {
            m_PredictionDist = prediction;
            foreach (var steeringAgent in GameObject.FindObjectsOfType<SteeringAgent>())
            {
                steeringAgent.SetPredictionFactor(prediction);
            }
        }

        private void Update()
        {
            if(Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(ray, out hit, 100))
                {
                    m_TargetPosition = hit.point;
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetSteeringMode("Seek");
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SetSteeringMode("Flee");
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SetSteeringMode("Pursuit");
            if (Input.GetKeyDown(KeyCode.Alpha4))
                SetSteeringMode("Evasion");
        }

        private void FixedUpdate()
        {
            Vector3 positionDiff = m_TargetPosition - m_Target.position;
            positionDiff.y = 0;
            m_Target.velocity = positionDiff.magnitude < m_TargetSpeed * Time.deltaTime ? Vector3.zero : positionDiff.normalized * m_TargetSpeed;

            for(int i = 0; i < agents.Length; i++)
            {
                agents[i].UpdatePhysics();
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (!m_EnableDebug)
                return;

            if (m_Target == null)
                return;

            var targetVelocity = m_Target.velocity;

            Gizmos.color = m_TargetColor;
            for(int p = 0; p < m_MaxDebugPoints; p++)
            {
                var targetPos = m_Target.position + targetVelocity * (p + 1) * Time.fixedDeltaTime;
                Gizmos.DrawWireSphere(targetPos, m_DebugSize);
            }

            Gizmos.color = m_AgentColor;
            for (int i = 0; i < agents.Length; i++)
            {
                var agent = agents[i];
                var currentVelocity = agent.Rigidbody.velocity;
                var currentPosition = agent.Rigidbody.position;
                var mass = agent.Rigidbody.mass;
                if (currentVelocity.magnitude <= Mathf.Epsilon)
                    continue;

                for (int p = 0; p < m_MaxDebugPoints; p++)
                {
                    var targetPos = m_Target.position + targetVelocity * p * Time.fixedDeltaTime;
                    var targetPredictedPos = m_Target.position + targetVelocity * (p + 1) * Time.fixedDeltaTime;
                    var steeringDirection = agent.GetSteeringDirection(currentPosition, currentVelocity, targetPos, targetPredictedPos);
                    currentVelocity = agent.CalculateVelocity(steeringDirection, currentVelocity, mass);

                    currentPosition += currentVelocity * Time.fixedDeltaTime;
                    Gizmos.DrawWireSphere(currentPosition, m_DebugSize);
                }
            }
        }
    }
}
