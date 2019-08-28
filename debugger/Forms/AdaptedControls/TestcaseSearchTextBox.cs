using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace debugger.Forms
{
    public class TestcaseSearchTextBox : CustomToolStripTextBox
    {
        public Func<string[]> GetToSearch;
        public DelegateResultClicked ResultClicked;
        public delegate void DelegateResultClicked(string name);
        public TestcaseSearchTextBox(Func<string[]> getToSearch, DelegateResultClicked resultClicked, Layer layer, Emphasis emphasis) : base(layer, emphasis)
        {
            GetToSearch = getToSearch;
            ResultClicked = resultClicked;
            Prefix = "Search: ";
            TextAlign = ContentAlignment.MiddleLeft;
            DropDown.PreviewKeyDown += KeyPressed;
            DropDown.DefaultDropDownDirection = ToolStripDropDownDirection.BelowRight;
            DisplayStyle = ToolStripItemDisplayStyle.Text;
            DropDown.Opening += DropDown_Opening;
            Ready();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            DropDown.Hide();
            if (Input.Length > 1)
            {
                DropDown.Show();
            }
        }

        private void DropDown_Opening(object sender, CancelEventArgs e)
        {
            DropDown.Items.Clear();
            if (Input.Length > 1)
            {
                string[] ToSearch = GetToSearch.Invoke();
                for (int i = 0; i < ToSearch.Length; i++) // each tosearch string
                {
                    if (ToSearch[i].Length >= Input.Length) // dont bother if input is bigger than the string
                    {
                        for (int j = 0; j < Input.Length; j++) //each char in string
                        {
                            if (ToSearch[i][j] != Input[j]) break; //if they are different, the strings have a different prefix of size=input.length
                            if (j + 1 == Input.Length)//if this is the final iteration
                            {
                                var ToAdd = new ThemedToolStripMenuItem() { Text = ToSearch[i] };
                                ToAdd.Click += (s, a) => ResultClicked(s.ToString());
                                DropDown.Items.Add(ToAdd);//if we never broke, they were equal
                            }
                        }
                    }
                }
            }
            DropDown.Update();
        }
    }
}
