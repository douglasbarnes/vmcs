namespace debugger.Forms
{
    public class RegisterLabel : CustomLabel
    {
        public readonly bool NoFormat;
        public readonly bool ShowUpper;        
        public RegisterLabel(bool noFormat=false, bool showUpper=false) : base(Emphasis.High)
        {
            NoFormat = noFormat;
            ShowUpper = showUpper;
            Size = new System.Drawing.Size(150, 15);
        }
    }
}
