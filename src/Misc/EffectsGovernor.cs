using Godot;

namespace WolfUI;

// In auto mode, drops to reduced effects if the UI can't keep up with the display for a few seconds in a row.
public partial class EffectsGovernor : Node
{
	private const double WarmupSeconds = 6.0;
	private const int StrikesAllowed = 4;
	private static readonly double TargetFps =
		double.TryParse(System.Environment.GetEnvironmentVariable("BASSETS_GOVERNOR_TARGET"), out var t) ? t : 60.0;

	private double _elapsed;
	private double _sinceCheck;
	private int _strikes;

	public override void _Process(double delta)
	{
		if (!Effects.IsAuto || !Effects.IsFull) return;

		_elapsed += delta;
		if (_elapsed < WarmupSeconds) return;

		_sinceCheck += delta;
		if (_sinceCheck < 1.0) return;
		_sinceCheck = 0;

		_strikes = Engine.GetFramesPerSecond() < TargetFps * 0.7 ? _strikes + 1 : 0;
		if (_strikes >= StrikesAllowed)
			Effects.StepDown($"only {Engine.GetFramesPerSecond():0} fps");
	}
}
