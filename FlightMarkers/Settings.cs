namespace FlightMarkers
{
    public class Settings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Flight Markers"; } }
        public override string DisplaySection { get { return "Squidsoft Collective"; } }
        public override string Section { get { return "Squidsoft Collective"; } }
        public override int SectionOrder { get { return 1; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomIntParameterUI("Lift Cutoff",
            toolTip = "Lower value sets the arrow sooner and keeps it on longer.")]
        public int LiftCutoff = 10;

        [GameParameters.CustomIntParameterUI("Body Lift Cutoff",
            toolTip = "Lower value sets the arrow sooner and keeps it on longer.")]
        public int BodyLiftCutoff = 15;

        [GameParameters.CustomIntParameterUI("Drag Cutoff",
            toolTip = "Lower value sets the arrow sooner and keeps it on longer.")]
        public int DragCutoff = 10;

        [GameParameters.CustomParameterUI("Combine by default",
            toolTip = "If true, the lift arrows will combine by default.")]
        public bool DefaultCombine = true;

		[GameParameters.CustomParameterUI("Highlight on switch",
			toolTip = "If true, highlight the control part when switching.")]
		public bool HighlightOnSwitch = true;
    }
}
