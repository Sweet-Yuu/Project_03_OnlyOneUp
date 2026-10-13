using System;
using UnityEngine;

public static class NestEggPresentationValidation
{
    private static int _assertions;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _assertions++;
    }
    private static void Near(Vector3 actual, Vector3 expected, string message) =>
        Check(Vector3.Distance(actual, expected) < .001f, message);

    public static int Run()
    {
        _assertions = 0;
        var presenter = new NestEggPresentation();
        var frame = new NestEggFrame
        {
            Generation = 1, Phase = NestEggPhase.Intact, EggSupported = true, EggInNest = true,
            NestRotation = Quaternion.identity, EggRotation = Quaternion.identity,
            EggPosition = Vector3.up * 2
        };
        Pose nest = new Pose(Vector3.right * 4, Quaternion.identity);
        presenter.Reset(frame);
        var shown = presenter.Present(frame, nest, .016f);
        Near(shown.EggPosition, nest.position + Vector3.up * 2, "Supported egg uses displayed nest frame");
        for (int i = 1; i <= 60; i++)
        {
            frame.Time = i * .02;
            frame.EggSupported = i % 3 == 0;
            nest.position += Vector3.forward * .1f;
            nest.rotation = Quaternion.Euler(0, i, 0);
            shown = presenter.Present(frame, nest, .02f);
            Near(Quaternion.Inverse(nest.rotation) * (shown.EggPosition - nest.position), Vector3.up * 2,
                "Intermittent contacts must not alternate between host and predicted coordinates");
        }

        // Mất gói: cùng một sample không được làm hết thời gian đỡ dựa trên thời gian render.
        frame.EggSupported = false;
        for (int i = 0; i < 20; i++)
        {
            nest.position += Vector3.right * .1f;
            shown = presenter.Present(frame, nest, .016f);
            Near(shown.EggPosition, nest.position + nest.rotation * Vector3.up * 2,
                "Packet stall holds attachment until sample timeline advances");
        }

        Vector3 previous = shown.EggPosition;
        frame.Time += .2;
        frame.EggInNest = false;
        frame.EggPosition = new Vector3(0, 1, 0);
        shown = presenter.Present(frame, nest, 0);
        Near(shown.EggPosition, previous, "Detaching preserves displayed pose with a large nest offset");
        shown = presenter.Present(frame, nest, .016f);
        Check(Vector3.Distance(previous, shown.EggPosition) <= 20f * .016f + .001f,
            "Detach correction is bounded instead of jumping to host");
        for (int i = 0; i < 180; i++)
        {
            frame.Time += .016;
            shown = presenter.Present(frame, nest, .016f);
        }
        Near(shown.EggPosition, frame.EggPosition, "Fallen egg converges to host world trajectory");
        previous = shown.EggPosition;
        nest.position += Vector3.right * 10;
        shown = presenter.Present(frame, nest, .016f);
        Near(shown.EggPosition, previous, "Detached egg is not dragged along with moving nest");

        frame.EggSupported = true;
        frame.EggInNest = true;
        frame.Time += .02;
        shown = presenter.Present(frame, nest, 0);
        Near(shown.EggPosition, previous, "Reattachment also preserves current pose");
        frame.Phase = NestEggPhase.Broken;
        frame.Time += .02;
        shown = presenter.Present(frame, nest, 0);
        Near(shown.EggPosition, previous, "Breaking does not teleport the visual egg");
        Check(shown.Phase == NestEggPhase.Broken, "Host break decision is retained");

        frame.Generation++;
        frame.EggSupported = false;
        frame.EggInNest = false;
        frame.Phase = NestEggPhase.Intact;
        frame.EggPosition = Vector3.up * 20;
        shown = presenter.Present(frame, nest, 0);
        Near(shown.EggPosition, frame.EggPosition, "New generation resets old visual trajectory");

        // Bù cả cú đổi vị trí trong cùng hệ tọa độ do gói mới tới sau một khoảng mất gói.
        frame.EggPosition += Vector3.right * 5;
        previous = shown.EggPosition;
        shown = presenter.Present(frame, nest, .016f);
        Check(Vector3.Distance(shown.EggPosition, previous) <= .321f, "Resumed packet cannot teleport airborne egg");
        TestLongBounceAndContainment();
        return _assertions;
    }

    private static void TestLongBounceAndContainment()
    {
        var region = new NestEggContainment(new Bounds(Vector3.up, new Vector3(4, 4, 4)));
        var presenter = new NestEggPresentation();
        var frame = new NestEggFrame
        {
            Generation = 1, Phase = NestEggPhase.Intact, EggSupported = false,
            NestRotation = Quaternion.identity, EggRotation = Quaternion.identity,
            EggPosition = Vector3.up * 2, EggInNest = region.Evaluate(Vector3.up * 2)
        };
        presenter.Reset(frame);
        // Tái hiện: client đi liên tục, trứng không tiếp xúc hơn 120 ms nhưng vẫn ở trong tổ.
        for (int i = 1; i <= 120; i++)
        {
            frame.Time = i * .02;
            frame.EggInNest = region.Evaluate(Vector3.up * 2);
            Pose nest = new Pose(new Vector3(i * .1f, 0, 0), Quaternion.Euler(0, i, 0));
            var shown = presenter.Present(frame, nest, .02f);
            Near(Quaternion.Inverse(nest.rotation) * (shown.EggPosition - nest.position), Vector3.up * 2,
                "Long loss of contact inside nest must not eject visual egg while client moves");
        }
        Check(region.Evaluate(new Vector3(2.1f, 1, 0)), "Exit margin prevents edge flicker");
        Check(!region.Evaluate(new Vector3(2.3f, 1, 0)), "Real exit leaves containment");
        Check(!region.Evaluate(new Vector3(2.1f, 1, 0)), "Outer margin alone cannot reattach fallen egg");
        Check(region.Evaluate(Vector3.up), "Actual return inside can reattach");
        Check(!region.Evaluate(Vector3.down * 2), "Egg below floor must detach");
        region.Reset();
        Check(!region.Evaluate(new Vector3(2.1f, 1, 0)), "Reset clears containment hysteresis");

        var buffer = new NestEggFrameBuffer();
        frame.Time = 1;
        frame.EggInNest = true;
        frame.EggPosition = new Vector3(1, 2, 0);
        var next = frame;
        next.Time = 2;
        next.NestRotation = Quaternion.Euler(0, 120, 0);
        next.EggPosition = next.NestRotation * frame.EggPosition;
        buffer.Add(frame); buffer.Add(next);
        buffer.Sample(1.5, out var middle, true);
        Near(Quaternion.Inverse(middle.NestRotation) * (middle.EggPosition - middle.NestPosition),
            frame.EggPosition, "Airborne egg inside nest interpolates locally despite no contacts");
    }
}
