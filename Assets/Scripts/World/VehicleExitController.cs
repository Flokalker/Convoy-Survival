using UnityEngine;

namespace PostApocRoadtrip.World
{
    public class VehicleExitController : MonoBehaviour
    {
        public float enterDistance = 4.2f;

        private PreviewCarController carController;
        private FirstPersonSurvivorController survivorController;
        private GameObject survivorObject;
        private bool inVehicle = true;
        private float nextToggleTime;

        private void Awake()
        {
            carController = GetComponent<PreviewCarController>();
            CreateSurvivor();
        }

        private void Update()
        {
            if (Time.time < nextToggleTime || !Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            nextToggleTime = Time.time + 0.35f;
            if (inVehicle)
            {
                ExitVehicle();
            }
            else if (survivorObject != null && Vector3.Distance(survivorObject.transform.position, transform.position) <= enterDistance)
            {
                EnterVehicle();
            }
        }

        private void ExitVehicle()
        {
            CreateSurvivor();
            inVehicle = false;
            carController.Halt();
            carController.enabled = false;
            survivorObject.transform.position = transform.position + transform.right * 3f + Vector3.up * 0.15f;
            survivorObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            survivorObject.SetActive(true);
        }

        private void EnterVehicle()
        {
            inVehicle = true;
            survivorObject.SetActive(false);
            carController.enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CreateSurvivor()
        {
            if (survivorObject != null)
            {
                return;
            }

            survivorObject = new GameObject("FirstPersonSurvivor");
            survivorObject.SetActive(false);
            survivorObject.transform.position = transform.position + transform.right * 3f;
            var characterController = survivorObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.34f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            survivorController = survivorObject.AddComponent<FirstPersonSurvivorController>();

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(0.24f, 0.27f, 0.28f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "SurvivorBody";
            body.transform.SetParent(survivorObject.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.62f, 0.86f, 0.62f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(body.GetComponent<Collider>());
        }

        private void OnGUI()
        {
            var text = inVehicle ? "E: aussteigen" : "E nahe am Van: einsteigen   |   Linksklick: schiessen   |   Shift: sprinten";
            GUI.color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
            GUI.DrawTexture(new Rect(Screen.width - 430f, 22f, 400f, 34f), Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.95f, 1f);
            GUI.Label(new Rect(Screen.width - 416f, 30f, 380f, 24f), text);
            GUI.color = Color.white;
        }
    }
}
