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

                // Jugador 0: Teclado/Mouse siempre disponible (se combina con gamepad si está conectado)
                if (i == 0)
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

                    // Si hay gamepad conectado, el stick izquierdo puede mover al jugador 0 también
                    if (pad.IsConnected)
                    {
                        Vector2 padMove = ApplyRadialDeadzone(pad.ThumbSticks.Left, GameConstants.LeftStickDeadzone);
                        padMove = new Vector2(padMove.X, -padMove.Y);
                        if (padMove != Vector2.Zero)
                        {
                            moveDir = padMove;
                        }
                    }
                    state.MoveDirection = moveDir;

                    // Apuntado: Mouse respecto al centro de la pantalla
                    Vector2 mousePos = new Vector2(mouse.X, mouse.Y);
                    Vector2 screenCenter = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
                    Vector2 mouseAim = mousePos - screenCenter;

                    if (mouseAim != Vector2.Zero)
                    {
                        state.AimDirection = Vector2.Normalize(mouseAim);
                    }

                    // Teclado (IJKL) tiene prioridad sobre el mouse si se presiona
                    if (kbd.IsKeyDown(Keys.I) || kbd.IsKeyDown(Keys.NumPad8)) state.AimDirection = new Vector2(0, -1);
                    else if (kbd.IsKeyDown(Keys.K) || kbd.IsKeyDown(Keys.NumPad2)) state.AimDirection = new Vector2(0, 1);
                    else if (kbd.IsKeyDown(Keys.J) || kbd.IsKeyDown(Keys.NumPad4)) state.AimDirection = new Vector2(-1, 0);
                    else if (kbd.IsKeyDown(Keys.L) || kbd.IsKeyDown(Keys.NumPad6)) state.AimDirection = new Vector2(1, 0);

                    // Gamepad (stick derecho) tiene prioridad sobre teclado/mouse si se mueve
                    if (pad.IsConnected)
                    {
                        Vector2 padAim = ApplyRadialDeadzone(pad.ThumbSticks.Right, GameConstants.RightStickDeadzone);
                        padAim = new Vector2(padAim.X, -padAim.Y);
                        if (padAim != Vector2.Zero)
                        {
                            state.AimDirection = padAim;
                        }
                    }

                    // Garantizar dirección válida por defecto si es Zero
                    if (state.AimDirection == Vector2.Zero)
                    {
                        state.AimDirection = new Vector2(1, 0);
                    }

                    // Disparo: Espacio, Click Izquierdo, o Gatillo Derecho / AutoShoot del Gamepad
                    bool triggerShootPad = pad.IsConnected && (pad.Triggers.Right >= GameConstants.TriggerShootThreshold);
                    bool stickShootPad = pad.IsConnected && GameConstants.AutoShootOnAim && 
                                      (pad.ThumbSticks.Right.LengthSquared() >= GameConstants.RightStickDeadzone * GameConstants.RightStickDeadzone);
                    
                    state.IsShooting = kbd.IsKeyDown(Keys.Space) || 
                                       mouse.LeftButton == ButtonState.Pressed || 
                                       triggerShootPad || 
                                       stickShootPad;
                    
                    // Escudo: Shift, o Botón A del Gamepad
                    bool shieldHeldKbd = kbd.IsKeyDown(Keys.LeftShift) || kbd.IsKeyDown(Keys.RightShift) || 
                                         (pad.IsConnected && pad.Buttons.A == ButtonState.Pressed);
                    
                    state.ShieldPressedThisFrame = shieldHeldKbd && !_shieldHeldPrevious[i];
                    state.ShieldHeld = shieldHeldKbd;
                    _shieldHeldPrevious[i] = shieldHeldKbd;

                    continue;
                }

                state.IsConnected = pad.IsConnected;

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
                bool stickShoot = GameConstants.AutoShootOnAim && rightRaw.LengthSquared() >=
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
