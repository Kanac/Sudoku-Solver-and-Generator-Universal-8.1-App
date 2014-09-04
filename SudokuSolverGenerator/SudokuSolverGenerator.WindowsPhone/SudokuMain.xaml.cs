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
        private const string VALUESFILE = "values.json";
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
            Create_Cells();
            Create_Boxes();
            try
            {
                Create_Sudoku((int)e.Parameter);
            }
            catch
            {
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
                    _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.White);
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

            _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);      //Remove highlight background if any

            if (!(_values[_selectedIndex].Text.Contains(valueChoice.Text)))         //Algorithm to manage values in a cell
            {
                var cellValues = _values[_selectedIndex].Text + valueChoice.Text;
                var charValues = cellValues.ToCharArray();
                Array.Sort(charValues);
                _values[_selectedIndex].Text = "";

                foreach (var a in charValues)
                {
                    _values[_selectedIndex].Text += a;
                }
            }

            if (_values[_selectedIndex].Text.Length == 1)
                _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.ForestGreen);       //Highlight foreground if assigned, otherwise background will cover number
            else
                _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.Gold);          //To indicate multiple candidates 

            if (_values.Where(c => c.Text.Any()).Count() == 81)         //Check if placement solves puzzle        
            {
                if (_values.Select(c => c.Text).SequenceEqual(puzzle.SolvedCells[0].Select(c => c[0].ToString())))
                {
                    VictoryMP3.Play();
                }
            }

        }

        private void Clear_Cell(object sender, TappedRoutedEventArgs e)
        {

            if (_selectedIndex == -1)
                return;

            if (_values[_selectedIndex].Text.Any())
            {
                _values[_selectedIndex].Text = "";
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

        private void Generate_Easy(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SudokuMain), 35);
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
}
