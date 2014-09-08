using Sudoku;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace SudokuSolverGenerator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SudokuSolver : Page
    {
        private int _selectedIndex = -1;
        private Random rand = new Random();
        private List<Border> _borders;
        private List<TextBlock> _values;
        private SudokuPuzzle puzzle;

        public SudokuSolver()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            if (_values == null)
            {
                Create_Cells();
                Create_Boxes();
            }

            puzzle = new SudokuPuzzle(9);
        }

        private void Create_Cells()
        {
            _borders = new List<Border>();
            _values = new List<TextBlock>();

            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {

                    var cell = new TextBlock();
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);

                    var border = new Border { BorderBrush = new SolidColorBrush(Colors.White), BorderThickness = new Thickness(1), Background = new SolidColorBrush(Colors.Black) };

                    border.Tapped += Cell_Tapped;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);

                    var viewBox = new Viewbox();
                    Grid.SetRow(viewBox, row);
                    Grid.SetColumn(viewBox, col);

                    SudokuSolverGrid.Children.Add(viewBox);
                    SudokuSolverGrid.Children.Add(border);
                    viewBox.Child = cell;
                }
            }

            _values = SudokuSolverGrid.Children.Where(c => c is Viewbox).Select(c => (TextBlock)((Viewbox)c).Child).ToList();
            _borders = SudokuSolverGrid.Children.Where(c => c is Border).Select(c => (Border)c).ToList();
        }

        private void Create_Boxes()
        {
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var box = new Border();
                    box.BorderThickness = new Thickness(5);
                    box.BorderBrush = new SolidColorBrush(Colors.White);
                    Grid.SetRow(box, row * 3);
                    Grid.SetColumn(box, col * 3);
                    Grid.SetRowSpan(box, 3);
                    Grid.SetColumnSpan(box, 3);
                    SudokuSolverGrid.Children.Add(box);
                }

            }
        }

        private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var border = (Border)sender;
            var newIndex = Grid.GetRow(border) * 9 + Grid.GetColumn(border);

            //Previous selection algorithim
            if (_selectedIndex != -1)
            {
                if (!(_values[_selectedIndex].Text.Any()))
                    _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);

                else 
                    _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.DodgerBlue);         //Leave cells with the font that marks multiple values 
            }

            //Current selection algorithim
            _selectedIndex = newIndex;

            if (_values[_selectedIndex].Text.Any())
                _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.ForestGreen);
            else
                _borders[_selectedIndex].Background = new SolidColorBrush(Colors.ForestGreen);

        }

        private async void Value_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_selectedIndex == -1 )
                return;

            var valueChoice = (TextBlock)sender;

            foreach (var peer in puzzle.FindPeers(_selectedIndex))
                if (valueChoice.Text == _values[peer].Text)
                {
                    await new MessageDialog("Contradictory Placement").ShowAsync();
                    return;
                }

            _borders[_selectedIndex].Background.ClearValue(SolidColorBrush.ColorProperty);      //Remove background highlight if any, since this cell is going to have at least 1 value, which will be highlighted instead
            _values[_selectedIndex].Text = valueChoice.Text;
            _values[_selectedIndex].Foreground = new SolidColorBrush(Colors.ForestGreen);
        }

        private void Go_To_Generator(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SudokuMain));
        }

        private async void Sudoku_Solve(object sender, TappedRoutedEventArgs e)
        {
            if (_values.Where(c => c.Text.Any()).ToList().Count < 17)
            {
                await new MessageDialog("Sudoku Puzzles require at least 17 cells for a unique solution").ShowAsync();
                return;
            }

            puzzle.Cells.Clear();
            puzzle.SolvedCells.Clear();

            for (int i = 0; i < puzzle.Length * puzzle.Length; i++)
            {
                if (_values[i].Text.Any())
                    puzzle.Cells.Add(new List<int> { Int32.Parse(_values[i].Text) });
                else
                    puzzle.Cells.Add(new List<int>(Enumerable.Range(1, puzzle.Length)));
            }

            if (puzzle.SolvedCells.Count == 1)
            {
                for (int i = 0; i < puzzle.Length * puzzle.Length; i++)
                {
                    _borders[i].Background.ClearValue(SolidColorBrush.ColorProperty);
                    if (!(_values[i].Text.Any()))
                        _values[i].Text = puzzle.SolvedCells[0][i][0].ToString();
                }

            }
            else if (puzzle.SolvedCells.Count > 1)
                await new MessageDialog("Invalid Puzzle given - multiple solutions").ShowAsync();
            else
                await new MessageDialog("Invalid Puzzle given - no solutions ").ShowAsync();
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

        private void Clear_Grid(object sender, RoutedEventArgs e)
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
    }
}
               

    

