using System;

// Ghép thời điểm bật nhảy của hai người khác nhau, không ghép thời điểm gói tin đến.
public sealed class CooperativeJumpWindow
{
    private readonly double _window;
    private double _host = double.NegativeInfinity;
    private double _guest = double.NegativeInfinity;
    private double _usedHost = double.NegativeInfinity;
    private double _usedGuest = double.NegativeInfinity;

    public CooperativeJumpWindow(double window) => _window = Math.Max(0, window);

    public bool Add(bool host, double time)
    {
        if (double.IsNaN(time) || double.IsInfinity(time)) return false;
        if (host)
        {
            if (time <= _host) return false;
            _host = time;
        }
        else
        {
            if (time <= _guest) return false;
            _guest = time;
        }
        if (_host <= _usedHost || _guest <= _usedGuest || Math.Abs(_host - _guest) > _window)
            return false;
        _usedHost = _host;
        _usedGuest = _guest;
        return true;
    }

    public void Reset()
    {
        _host = _guest = _usedHost = _usedGuest = double.NegativeInfinity;
    }
}
