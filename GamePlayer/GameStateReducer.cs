namespace GamePlayer;

using System;
using OpenTK.Mathematics;

public class GameStateReducer
{
    private readonly LevelData _levelData;

    public GameStateReducer(LevelData levelData)
    {
        _levelData = levelData;
    }

    public GameState Reduce(GameState currentState, InputState inputState)
    {
        const float maxSpeed = .26f;
        const float sameDirectionAcceleration = .1f / 4f;
        const float oppositeDirectionDeceleration = 1f / 8f;
        const float naturalDeceleration = .1f / 4f;
        const float gravity = .1f / 4f;
        const float jumpForce = .45f;

        if (!currentState.IsAlive)
        {
            return currentState with
            {
                CharacterDetails = currentState.CharacterDetails with
                {
                    Velocity = Vector2.Zero,
                }
            };
        }

        var newVelocity = new Vector2(
            MathF.Min(MathF.Max(inputState.LeftRightStatus switch
            {
                LeftRightStatus.Right when currentState.CharacterDetails.Velocity.X < 0.0f =>
                    currentState.CharacterDetails.Velocity.X + oppositeDirectionDeceleration,
                LeftRightStatus.Right =>
                    currentState.CharacterDetails.Velocity.X + sameDirectionAcceleration,
                LeftRightStatus.Left when currentState.CharacterDetails.Velocity.X > 0.0f =>
                    currentState.CharacterDetails.Velocity.X - oppositeDirectionDeceleration,
                LeftRightStatus.Left =>
                    currentState.CharacterDetails.Velocity.X - sameDirectionAcceleration,
                LeftRightStatus.None when currentState.CharacterDetails.Velocity.X > 0.0f =>
                    MathF.Max(currentState.CharacterDetails.Velocity.X - naturalDeceleration, 0.0f),
                LeftRightStatus.None when currentState.CharacterDetails.Velocity.X < 0.0f =>
                    MathF.Min(currentState.CharacterDetails.Velocity.X + naturalDeceleration, 0.0f),
                _ => 0.0f
            }, -maxSpeed), maxSpeed),
            inputState.JumpPressed && currentState.CharacterDetails.IsGrounded
                ? -jumpForce
                : currentState.CharacterDetails.Velocity.Y + gravity);

        var newPosition = new Vector2(
            currentState.CharacterDetails.Position.X + newVelocity.X,
            currentState.CharacterDetails.Position.Y + newVelocity.Y);

        bool DetectGround(float x) =>
            _levelData[(int)MathF.Floor(x), (int)MathF.Ceiling(newPosition.Y)] != '0';

        var isGrounded = DetectGround(newPosition.X + .1f) || DetectGround(newPosition.X + .9f);

        if (isGrounded)
        {
            newPosition = newPosition with { Y = MathF.Floor(newPosition.Y) };
            newVelocity = newVelocity with { Y = 0f };
        }

        var isInBlock =
            _levelData[(int)MathF.Floor(newPosition.X), (int)MathF.Floor(newPosition.Y)] != '0'
            || _levelData[(int)MathF.Ceiling(newPosition.X), (int)MathF.Floor(newPosition.Y)] != '0';

        if (isInBlock)
        {
            newPosition = new Vector2(
                newVelocity.X > 0 ? MathF.Floor(newPosition.X) : MathF.Ceiling(newPosition.X),
                newPosition.Y);

            newVelocity = new Vector2(0f, newVelocity.Y);
        }

        newPosition = new Vector2(
            MathF.Max(currentState.HorizontalScroll / 16f, newPosition.X),
            newPosition.Y);

        var isAlive = currentState.IsAlive && newPosition.Y <= 15;

        return currentState with
        {
            CharacterDetails = new(newPosition, newVelocity, isGrounded),
            IsAlive = isAlive,
        };
    }
}