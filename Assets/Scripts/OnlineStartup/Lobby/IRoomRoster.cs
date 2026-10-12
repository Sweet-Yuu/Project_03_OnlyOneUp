using System;
using System.Collections.Generic;

public interface IRoomRoster
{
    event Action Changed;
    IReadOnlyList<RoomMember> Members { get; }
    string Code { get; }
}
