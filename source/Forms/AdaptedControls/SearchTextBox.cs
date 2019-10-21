// SearchTextBox allows a string array to be searched and displayed suitably. In the current implementation, it is used to search testcases.
// It takes a delegate to return a string array(to ensure the string array is always up to date), and a delegate when a result is clicked.
// The searching method is to search by prefix.
// Let $MyString be the string to be searched and $Input to be the user input.
// If the first $Input.Length characters of $MyString are the same as $Input, the result is true, otherwise false.
using System;
namespace debugger.Forms
{
    public class SearchTextBox : CustomToolStripTextBox
    {
        // The delegate used to fetch all search results.
        public delegate string[] GetToSearchDelegate();
        private readonly GetToSearchDelegate GetToSearch;

        // A delegate and event pair for when a result is clicked on
        public event DelegateResultClicked OnResultClicked = (name) => { };
        public delegate void DelegateResultClicked(string name);

        public SearchTextBox(GetToSearchDelegate getToSearch, Layer layer, Emphasis emphasis) : base(layer, emphasis)
        {
            GetToSearch = getToSearch;

            // See CustomToolsStripTextBox
            Prefix = "Search: ";

            // Add this event to the inherited keypressed event(See CustomToolStripTextBox).
            DropDown.PreviewKeyDown += KeyPressed;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            // This will redraw the searchbox with the new text.
            base.OnTextChanged(e);
            
            // Only display results when the input length is greater than one to narrow the search.
            if (Input.Length > 1)
            {
                int ResultCount = 0;
                string[] ToSearch = GetToSearch.Invoke();
                for (int i = 0; i < ToSearch.Length; i++) // each tosearch string
                {
                    if (ToSearch[i].Length >= Input.Length) // dont bother if input is bigger than the string
                    {
                        for (int j = 0; j < Input.Length; j++) //each char in string
                        {
                            if (ToSearch[i][j] != Input[j]) break; //if they are different, the strings have a different prefix of size=input.length

                            // If this is the final iteration and the loop has not broken, the string must match(see summary).
                            if (j + 1 == Input.Length)
                            {
                                // Very strange windows forms phenomena will happen if this is paradigm is not used.
                                // Its not the worst news though, as this is more efficient. See MemoryListView or DisassemblyListView for 
                                // more implementations of this. However as opposed to the reasons there, this is necessary.
                                // Pay attention to the top left corner, https://vimeo.com/367460328 password i-love-windows-forms (Javascript required)      
                                // If you cannot open the video, all that happens is the search results appear in the top left corner of the screen. Yes, the screen
                                // not even the window. 
                                if (DropDown.Items.Count == ResultCount)
                                {                                    
                                    // Create a new menu item to be placed
                                    var ToAdd = new ThemedToolStripMenuItem() { Text = ToSearch[i] };

                                    // Set its event to invoke OnResultClicked with its text. This also comes in handy for when the text is changed(see else)
                                    ToAdd.Click += (s, a) => OnResultClicked.Invoke(ToAdd.Text);

                                    DropDown.Items.Add(ToAdd);
                                }
                                else
                                {
                                    // Change the text to the new search result.
                                    DropDown.Items[ResultCount].Text = ToSearch[i];
                                }
                                
                            }
                        }
                    }
                }

                // As there are results, show the dropdown.
                DropDown.Show();
            }
            else
            {
                // Hide as there is no longer enough input to deduce accurate results.
                DropDown.Hide();
            }
        }
    }
}
