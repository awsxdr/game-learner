namespace GamePlayer;

using System.Collections.Generic;
using System.Linq;

public static class InputStateSerializer
{
    public static IEnumerable<byte> Serialize(IEnumerable<InputState> states) =>
        states.Select(Serialize);

    public static byte Serialize(InputState state) =>
        (byte) (
            (byte) (state.JumpPressed ? 0b100 : 0)
            | (byte) state.LeftRightStatus);

    public static IEnumerable<InputState> Deserialize(IEnumerable<byte> data) =>
        data.Select(Deserialize);

    public static InputState Deserialize(byte data) =>
        new((LeftRightStatus) (data & 0b11), (data & 0b100) != 0);
}