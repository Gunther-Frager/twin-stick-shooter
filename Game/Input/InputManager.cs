using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TwinStickShooter.Core;

namespace TwinStickShooter.Input
{
    /// <summary>
    /// Polling centralizado de input para hasta MaxPlayers mandos.
    /// Todo el estado vive en arrays fijos creados una sola vez (Initialize),
    /// así Update() no alloca nada en cada frame.
    /// </summary>
    public class InputManager
    {
        private readonly PlayerInputState[] _current;
        private readonly bool[] _shieldHeldPrevious;

        public InputManager()
        {
            _current = new PlayerInputState[GameConstants.MaxPlayers];
            _shieldHeldPrevious = new bool[GameConstants.MaxPlayers];
        }

        /// <summary>Snapshot de solo lectura del estado actual de un jugador.</summary>
        public PlayerInputState GetState(int playerIndex) => _current[playerIndex];

        public void Update()
        {
            for (int i = 0; i < GameConstants.MaxPlayers; i++)
            {
                GamePadState pad = GamePad.GetState((PlayerIndex)i);

                ref PlayerInputState state = ref _current[i];
                state.IsConnected = pad.IsConnected;

                // Fallback: Si es el jugador 0 y no hay gamepad conectado, habilitar teclado
                if (i == 0 && !pad.IsConnected)
                {
                    state.IsConnected = true;
                    var kbd = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                    var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

                    Vector2 moveDir = Vector2.Zero;
                    if (kbd.IsKeyDown(Keys.W) || kbd.IsKeyDown(Keys.Up)) moveDir.Y -= 1f;
                    if (kbd.IsKeyDown(Keys.S) || kbd.IsKeyDown(Keys.Down)) moveDir.Y += 1f;
                    if (kbd.IsKeyDown(Keys.A) || kbd.IsKeyDown(Keys.Left)) moveDir.X -= 1f;
                    if (kbd.IsKeyDown(Keys.D) || kbd.IsKeyDown(Keys.Right)) moveDir.X += 1f;

                    if (moveDir != Vector2.Zero) moveDir.Normalize();
                    state.MoveDirection = moveDir;

                    // Apuntado con mouse respecto al centro de la pantalla o posición del jugador
                    // Por simplicidad, apuntado con flechas o mouse si se desea. Aquí usaremos flechas o mouse.
                    Vector2 aimDir = new Vector2(1f, 0f); // default
                    if (kbd.IsKeyDown(Keys.NumPad8) || kbd.IsKeyDown(Keys.I)) aimDir = new Vector2(0f, -1f);
                    else if (kbd.IsKeyDown(Keys.NumPad2) || kbd.IsKeyDown(Keys.K)) aimDir = new Vector2(0f, 1f);
                    else if (kbd.IsKeyDown(Keys.NumPad4) || kbd.IsKeyDown(Keys.J)) aimDir = new Vector2(-1f, 0f);
                    else if (kbd.IsKeyDown(Keys.NumPad6) || kbd.IsKeyDown(Keys.L)) aimDir = new Vector2(1f, 0f);
                    
                    if (aimDir != Vector2.Zero) state.AimDirection = aimDir;

                    state.IsShooting = kbd.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
                    
                    bool shieldHeldKbd = kbd.IsKeyDown(Keys.LeftShift) || kbd.IsKeyDown(Keys.RightShift);
                    state.ShieldPressedThisFrame = shieldHeldKbd && !_shieldHeldPrevious[i];
                    state.ShieldHeld = shieldHeldKbd;
                    _shieldHeldPrevious[i] = shieldHeldKbd;

                    continue;
                }

                if (!pad.IsConnected)
                {
                    state.MoveDirection = Vector2.Zero;
                    state.AimDirection = Vector2.Zero;
                    state.IsShooting = false;
                    state.ShieldHeld = false;
                    state.ShieldPressedThisFrame = false;
                    _shieldHeldPrevious[i] = false;
                    continue;
                }

                state.MoveDirection = ApplyRadialDeadzone(pad.ThumbSticks.Left, GameConstants.LeftStickDeadzone);

                // En MonoGame, Y+ del stick es "arriba"; nuestro mundo usa Y+ "abajo" (coords de pantalla).
                state.MoveDirection = new Vector2(state.MoveDirection.X, -state.MoveDirection.Y);

                Vector2 rightRaw = pad.ThumbSticks.Right;
                Vector2 aim = ApplyRadialDeadzone(rightRaw, GameConstants.RightStickDeadzone);
                aim = new Vector2(aim.X, -aim.Y);

                if (aim != Vector2.Zero)
                {
                    state.AimDirection = aim; // ya normalizado por ApplyRadialDeadzone
                }
                // Si no hay input en el stick derecho, se mantiene la última dirección
                // de apuntado (comportamiento típico de twin-stick shooters).

                bool triggerShoot = pad.Triggers.Right >= GameConstants.TriggerShootThreshold;
                bool stickShoot = rightRaw.LengthSquared() >=
                    GameConstants.RightStickDeadzone * GameConstants.RightStickDeadzone;
                state.IsShooting = triggerShoot || stickShoot;

                bool shieldHeld = pad.Buttons.A == ButtonState.Pressed;
                state.ShieldPressedThisFrame = shieldHeld && !_shieldHeldPrevious[i];
                state.ShieldHeld = shieldHeld;
                _shieldHeldPrevious[i] = shieldHeld;
            }
        }

        /// <summary>
        /// Deadzone radial (no por eje): más natural para apuntado 360°,
        /// evita el "cuadrado" de deadzone por eje y remapea [deadzone,1] -> [0,1].
        /// </summary>
        private static Vector2 ApplyRadialDeadzone(Vector2 stick, float deadzone)
        {
            float length = stick.Length();
            if (length <= deadzone)
            {
                return Vector2.Zero;
            }

            float normalizedLength = (length - deadzone) / (1f - deadzone);
            normalizedLength = MathHelper.Clamp(normalizedLength, 0f, 1f);

            return stick / length * normalizedLength;
        }
    }
}
