using Sudoku;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SudokuSolverGenerator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    
    public sealed partial class SudokuMain : Page
    {
        private int _selectedIndex = -1;
        private Random rand = new Random();
        private List<Border> _borders;
        private List<TextBlock> _values;
        private SudokuPuzzle puzzle;

        

        public SudokuMain()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            if (puzzle == null)
            {
                Create_Cells();
                Create_Boxes();
                Create_Sudoku(30);
            }
        }

        private void Create_Sudoku(int numCells)
        {
            puzzle = new SudokuPuzzle(9);
            puzzle.GenerateSudoku(numCells);

            for (var startingCell = 0; startingCell < 81; startingCell++ )
            {
                if (puzzle.Cells[startingCell].Count == 1)
                {
                    _borders[startingCell].Background.ClearValue(SolidColorBrush.ColorProperty);
                    _values[startingCell].Text = puzzle.Cells[startingCell][0].ToString();
                    _values[startingCell].Foreground = new SolidColorBrush(Colors.DodgerBlue);
                }

            }

        }
        private void Create_Cells()
        {
            _values = new List<TextBlock>();
            _borders = new List<Border>();

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {

                    var cell = new TextBlock ();
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);

                    var border = new Border { BorderBrush = new SolidColorBrush(Colors.White), BorderThickness = new Thickness(0.5), Background = new SolidColorBrush(Colors.Black) };

                    border.Tapped += Cell_Tapped;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);

                    var viewBox = new Viewbox();
                    Grid.SetRow(viewBox, row);
                    Grid.SetColumn(viewBox, col);

                    SudokuGrid.Children.Add(viewBox);
                    SudokuGrid.Children.Add(border);
                    viewBox.Child = cell;
                }
            }

            _values = SudokuGrid.Children.Where(c => c is Viewbox).Select(c => (TextBlock)((Viewbox)c).Child).ToList();
            _borders = SudokuGrid.Children.Where(c => c is Border).Select(c => (Border)c).ToList();
        }

        private void Create_Boxes()
        {
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var box = new Border();
                    box.BorderThickness = new Thickness(2.5);
                    box.BorderBrush = new SolidColorBrush(Colors.White);
                    Grid.SetRow(box, row * 3);
                    Grid.SetColumn(box, col * 3);
                    Grid.SetRowSpan(box, 3);
                    Grid.SetColumnSpan(box, 3);
                    SudokuGrid.Children.Add(box);
                }

            }
        }

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var border = (Border)sender;        
            var newIndex = Grid.GetRow(border) * 9 + Grid.GetColumn(border); 

            if (puzzle.Cells[newIndex].Count == 1)      //Don't making starting cells clickable
                return;

            //Previous selection algorithim
            if (_selectedIndex != -1 )          
            {
                if (!(_values[_selectedIndex].Text.Any()))
                    _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);

                else if ((_values[_selectedIndex].Text.Count() == 1))
                    _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.White);         //Leave cells with the font that marks multiple values 
                else
                    _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.Gold);
            }

            //Current selection algorithim
            _selectedIndex = newIndex;

            if (_values[_selectedIndex].Text.Any())
                _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.ForestGreen);
            else
                _borders[_selectedIndex].Background = new SolidColorBrush(Colors.ForestGreen);

        }

        private void Value_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedIndex == -1)
                return;

            var valueChoice = (TextBlock)sender;

            _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);      //Remove background highlight if any, since this cell is going to have at least 1 value, which will be highlighted instead

            if ((_values[_selectedIndex].Text.Length == 0))
            {
                _values[_selectedIndex].Text = valueChoice.Text;
            }
            else if (!(_values[_selectedIndex].Text.Contains(valueChoice.Text)))         //Algorithm to manage values in a cell
            {
                var cellValues = _values[_selectedIndex].Text + valueChoice.Text;
                var charValues = cellValues.ToCharArray();
                Array.Sort(charValues);
                _values[_selectedIndex].Text = "";

                foreach (var a in charValues)
                    _values[_selectedIndex].Text += a;

            }

            _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.ForestGreen);

            if (_values.Select(c => c.Text).SequenceEqual(puzzle.SolvedCells[0].Select(c => c[0].ToString())))
            {
                VictoryMP3.Play();
            }


        }

        private void Clear_Cell(object sender, TappedRoutedEventArgs e)
        {

            if (_selectedIndex == -1)
                return;

            if (_values[_selectedIndex].Text.Any())
            {
                _values[_selectedIndex].Text = "";
                _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.White);
                _borders[_selectedIndex].Background = new SolidColorBrush(Colors.ForestGreen);
            }
                
                
        }

        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            int cell;

            do
            {
                cell = rand.Next(puzzle.Length * puzzle.Length);   //Repeat until a random cell with more than 1 value is found

            } while (_values[cell].Text == puzzle.SolvedCells[0][cell][0].ToString());

            _borders[cell].Background.ClearValue(SolidColorBrush.ColorProperty);
            _values[cell].Foreground = new SolidColorBrush(Colors.White);
            _values[cell].Text = puzzle.SolvedCells[0][cell][0].ToString();
        }

        private void ClearGrid()
        {
            foreach (var val in _values)
            {
                val.Foreground = new SolidColorBrush(Colors.White);
                val.Text = "";
            }

            if (_selectedIndex != -1)
                _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);
            else 
                _selectedIndex = -1;

        }


        private void Generate_Easy(object sender, RoutedEventArgs e)
        {
            ClearGrid();
            Create_Sudoku(39);
        }

        private void Generate_Medium(object sender, RoutedEventArgs e)
        {
            ClearGrid();
            Create_Sudoku(35);
        }

        private void Generate_Hard(object sender, RoutedEventArgs e)
        {
            ClearGrid();
            Create_Sudoku(30);
        }

        private void Generate_Ultra(object sender, RoutedEventArgs e)
        {
            ClearGrid();
            Create_Sudoku(27);
        }

        private void Go_To_Solver(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SudokuSolver));
        }

        private void Sudoku_Solve(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < puzzle.Length * puzzle.Length; i++)
            {
                _borders[i].Background.ClearValue(SolidColorBrush.ColorProperty);
                _values[i].Text = puzzle.SolvedCells[0][i][0].ToString();
            }
        }

        //private async Task SerializeValuesAsync()
        //{
        //    var serializer = new DataContractJsonSerializer(typeof(SudokuPuzzle));

        //    using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForWriteAsync(VALUESFILE, CreationCollisionOption.ReplaceExisting))
        //    {
        //        serializer.WriteObject(stream, puzzle);
        //    }

        //}

        //private async Task<bool> DeserializeValuesAsync()
        //{
        //    var serializer = new DataContractJsonSerializer(typeof(List<string>));

        //    try
        //    {
        //        using (var stream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(VALUESFILE))
        //        {
        //            puzzle = (SudokuPuzzle)serializer.ReadObject(stream);
        //        }

        //        return true;
        //    }
        //    catch       //If no saved file, get ready to generate new pzuzle
        //    {
        //        return false;
        //    }
        //}
  

    }

    //public class SudokuValueColour
    //{
    //    private static SolidColorBrush _defaultValue = new SolidColorBrush(Colors.White);
    //    private static SolidColorBrush _multipleValues = new SolidColorBrush(Colors.Gold);
    //    private static SolidColorBrush _selectedValue = new SolidColorBrush(Colors.ForestGreen);
    //    private static SolidColorBrush _startValue = new SolidColorBrush(Colors.DodgerBlue);

    //    public static SolidColorBrush DefaultValue { get { return _defaultValue; } }
    //    public static SolidColorBrush MultipleValues { get { return _multipleValues; } }
    //    public static SolidColorBrush SelectedValue { get { return _selectedValue; } }
    //    public static SolidColorBrush StartValue { get { return _startValue; } }
    //}
}
