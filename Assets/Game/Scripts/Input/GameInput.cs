using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    /// <summary>
    /// All bindings are declared in code (keyboard/mouse + gamepad), so no .inputactions asset is needed.
    /// Keyboard paths are physical key positions: "&lt;Keyboard&gt;/w" is Z on an AZERTY keyboard.
    /// </summary>
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public InputAction Move, LookMouse, LookStick, Jump, Sprint, Aim, Fire, Reload,
            Interact, Neutralize, Pause, Navigate, Submit, Cancel;

        /// <summary>True when the last device the player touched was a gamepad.</summary>
        public bool UsingGamepad { get; private set; }

        public bool GameplayEnabled { get; set; } = true;

        InputAction[] all;

        void Awake()
        {
            Instance = this;

            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");

            LookMouse = new InputAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            LookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Aim = Button("Aim", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Fire = Button("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonNorth");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            Neutralize = Button("Neutralize", "<Keyboard>/f", "<Gamepad>/buttonEast");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");

            Navigate = new InputAction("Navigate", InputActionType.Value);
            Navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Navigate.AddBinding("<Gamepad>/dpad");
            Navigate.AddBinding("<Gamepad>/leftStick");

            Submit = Button("Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            Submit.AddBinding("<Keyboard>/numpadEnter");
            Cancel = Button("Cancel", "<Keyboard>/backspace", "<Gamepad>/buttonEast");

            all = new[] { Move, LookMouse, LookStick, Jump, Sprint, Aim, Fire, Reload,
                Interact, Neutralize, Pause, Navigate, Submit, Cancel };
            foreach (var a in all) a.Enable();
        }

        static InputAction Button(string name, string keyboardPath, string gamepadPath)
        {
            var action = new InputAction(name, InputActionType.Button);
            action.AddBinding(keyboardPath);
            action.AddBinding(gamepadPath);
            return action;
        }

        void Update()
        {
            foreach (var a in all)
            {
                var control = a.activeControl;
                if (control == null || control.device == null) continue;
                if (control.device is Gamepad) UsingGamepad = true;
                else if (control.device is Keyboard || control.device is Mouse)
                {
                    // Tiny mouse jitter should not flip the prompts back to keyboard.
                    if (a == LookMouse && a.ReadValue<Vector2>().sqrMagnitude < 4f) continue;
                    UsingGamepad = false;
                }
            }
        }

        void OnDestroy()
        {
            if (all != null)
                foreach (var a in all) a.Dispose();
            if (Instance == this) Instance = null;
        }

        public static bool HasAnyDevice => Keyboard.current != null || Gamepad.current != null;

        // Gameplay accessors return neutral values while a menu or cutscene owns the input.
        public Vector2 MoveValue => GameplayEnabled ? Vector2.ClampMagnitude(Move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        public bool Held(InputAction a) => GameplayEnabled && a.IsPressed();
        public bool Pressed(InputAction a) => GameplayEnabled && a.WasPressedThisFrame();

        /// <summary>Camera rotation in degrees for this frame (x = yaw, y = pitch).</summary>
        public Vector2 LookDelta()
        {
            if (!GameplayEnabled) return Vector2.zero;
            var mouse = LookMouse.ReadValue<Vector2>() * GameSession.MouseSensitivity;
            var stick = LookStick.ReadValue<Vector2>();
            stick = stick * stick.magnitude; // finer control near the centre
            stick *= GameSession.StickSensitivity * Time.unscaledDeltaTime;
            var look = mouse + stick;
            if (GameSession.InvertY) look.y = -look.y;
            return look;
        }
    }
}
