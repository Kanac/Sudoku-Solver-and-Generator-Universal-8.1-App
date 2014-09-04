using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sudoku
{
    
    public class SudokuPuzzle
    {
        private List<List<int>> _cells = new List<List<int>>();
        public List<List<int>> Cells { get { return _cells;} set {_cells = value;} }

        private List<List<List<int>>> _solvedCells = new List<List<List<int>>>();
        public List<List<List<int>>> SolvedCells { get { return _solvedCells; } set { _solvedCells = value; } }

        public int Length { get; set; }
        public int BoxSize { get { return (int)Math.Sqrt(Length); } }
        

        public SudokuPuzzle(int length)
        {
            this.Length = length;
            
        }

        private bool CheckIfSolved(List<List<int>> cells)
        {
            foreach (var cell in cells)
            {
                if (cell.Count() != 1)
                    return false;
            }

            return true;
        }


        private void SolveSudoku(int numSolutions)
        {
            var workingCells = new List<List<int>>();
            CopyCells(workingCells, Cells);    //Keep the starting puzzle and working solution separate

            EliminateCandidates(workingCells, numSolutions);
        }


        private void EliminateCandidates(List<List<int>> cells, int numSolutions)
        {
            for (int cell = 0; cell < Length * Length; cell++)
            {
                if (cells[cell].Count > 1)
                {
                    if (FindCandidates(cells, cell) == false)
                        return;
                }
            }

            if (!(CheckIfSolved(cells)))
                TestCandidate(cells, numSolutions);     //If puzzle not solved yet, test candidates
            else
            {
                Debug.WriteLine("Solution Found");
                SolvedCells.Add(cells);
            }
        }

        private void TestCandidate(List<List<int>> cells, int numSolutions)        //Tests every allowed path in case of multiple solutions
        {
            int minCandidates = cells.Where(cell => cell.Count > 1).Min(cands => cands.Count);  //Find the lowest amount of candidates of all cells
            int minCell = cells.FindIndex(cell => cell.Count == minCandidates);     //Find a cell that has that lowest number of candidates

            foreach (int candidate in cells[minCell])
            {
                Debug.WriteLine("Testing Candidate");
                var testCells = new List<List<int>>();
                CopyCells(testCells, cells);         //Save cells before testing           

                testCells[minCell] = new List<int> { candidate };
                if (!(UpdateCandidates(testCells, minCell)))        //If this candidate causes contradiction, move to next one
                    continue;

                EliminateCandidates(testCells, numSolutions);     //Have to assign testCells because successive testCells may get held (after making a copy from which the program proceeds with instead) when reversing after solving

                if (SolvedCells.Count() == numSolutions)
                    return;
            }

            if (!(CheckIfSolved(cells)))        //If at end of candidates list and not solved, candidate of previous stack frame is contradictory
                return;             //Using checkIfSolved since if found 1 solution, still want to check specific cells for other possible solutions 
        }

        public void CopyCells(List<List<int>> testCells, List<List<int>> Cells)
        {
            testCells.Clear();

            for (int i = 0; i < Length * Length; i++)     //Deep copy to create seperate reference 
            {
                testCells.Add(new List<int>());

                foreach (int cand in Cells[i])
                {
                    testCells[i].Add(cand);
                }
            }
        }

        private List<int> FindPeers(int cell)  //For a cell, find its related row, column and box peers. (Values will never change throughout)
        {
            var peers = new List<int>();

            for (int peerIndex = 0; peerIndex < Length * Length; peerIndex++)
            {
                if (peerIndex / Length == cell / Length  //If in same row, add to peers
                    | peerIndex % Length == cell % Length  //If in same column, add to peers
                    | (peerIndex / Length / BoxSize == cell / Length / BoxSize && peerIndex % Length / BoxSize == cell % Length / BoxSize) //If in same box, add to peers
                    && peerIndex != cell)  //Can't be same value for peer and cell
                    peers.Add(peerIndex);
            }

            return peers;
        }

        private bool FindCandidates(List<List<int>> cells, int cell)  //Find the possible allowed values for a cell, judging by its peers
        {
            var peers = FindPeers(cell);

            foreach (var peer in peers)
            {
                if (cells[peer].Count == 1 && cells[cell].Contains(cells[peer][0]))
                {
                    var cellCount = cells[cell].Count;
                    cells[cell].Remove(cells[peer][0]);

                    if (cellCount == 2)
                    {
                        if (!(UpdateCandidates(cells, cell)))
                            return false;

                        break;
                    }
                }
            }

            return true;
        }

        private bool UpdateCandidates(List<List<int>> cells, int cell)      //If a cell is solved through FindCandidates, update its peers candidates, successively
        {
            var peers = FindPeers(cell);

            foreach (var peer in peers)
            {
                if (cells[peer].Contains(cells[cell][0]))
                {
                    var peerLength = cells[peer].Count();

                    if (peerLength == 1)            //Contradiction -- false solution thus far
                        return false;

                    cells[peer].Remove(cells[cell][0]);

                    if (peerLength == 2)
                    {
                        if (!(UpdateCandidates(cells, peer)))
                            return false;
                    }
                }
            }

            return true;
        }



        public void GenerateSudoku(int numStartingCells)
        {
            var rand = new Random();        //Don't put rand inside AssignRandoCandidate since each will create new random objects in quick succession with the same seed - use 1 random object only
            var eliminatedCells = new List<int>();

            do
            {
                Debug.WriteLine("Trying new puzzle");
                Cells = Enumerable.Repeat(new List<int>(Enumerable.Range(1, 9)), Length * Length).ToList();
                SolvedCells.Clear();
                eliminatedCells.Clear();

                for (int randCell = 0; randCell < numStartingCells; randCell++)     //Iterate and add starting cells to sudoku puzzle
                {
                    if (CheckIfSolved(Cells))
                        break;

                    eliminatedCells.AddRange(AssignRandomCandidate(rand, Cells));      //Assigns the rand cand and then adds the eliminated cells as a result of this to the list

                    if (eliminatedCells.Contains(-1))
                        break;

                }

                if (eliminatedCells.Contains(-1))
                    continue;

                SolveSudoku(2);

            } while (SolvedCells.Count() != 1);

            Debug.WriteLine("Eliminated: {0}", eliminatedCells.Count());

            foreach (int c in eliminatedCells)      //Get rid of the excess solved cells from generatedSudoku, for the final puzzle to the user
            {
                if (Cells.Where(d => d.Count() == 1).Count() == numStartingCells)
                    break;

                Cells[c] = new List<int>(Enumerable.Range(1, Length));
            }

        }

        private List<int> AssignRandomCandidate(Random rand, List<List<int>> cells)
        {

            int cell;
            var assignedCells = new List<List<int>>();      //The cells prior to assigning random candidate and possible peers being solved due to this
            var eliminatedCells = new List<int>();

            CopyCells(assignedCells, cells);

            do
            {
                cell = rand.Next(Length * Length);   //Repeat until a random cell with more than 1 value is found

            } while (cells[cell].Count() == 1);

            var candidate = cells[cell][rand.Next(cells[cell].Count())];    //Finds a random candidate of the random cell and stores it

            assignedCells[cell] = new List<int> { candidate };

            if (!(UpdateCandidates(assignedCells, cell)))           //If assignment leads to contradiction, restart entire sudoku grid
            {
                eliminatedCells.Add(-1);
                return eliminatedCells;
            }

            for (int c = 0; c < Length * Length; c++)        //Find the eliminated cells due to random candidate assignment so that they are not included in the puzzle for the user later
            {
                if (assignedCells[c].Count == 1 && cells[c].Count > 1 && c != cell)
                    eliminatedCells.Add(c);
            }

            CopyCells(cells, assignedCells);
            return eliminatedCells;
        }

        public void PrintSudoku(List<List<int>> Cells)
        {
            for (int w = 0; w < Length * Length; w++)
            {
                if (w % Length == 0)
                    Debug.WriteLine("");
                if (Cells[w].Count() == 1)
                    Debug.WriteLine("{0} ", Cells[w][0]);
                else if (Cells[w].Count() == 0)
                    Debug.WriteLine(". ");
                else
                    Debug.WriteLine("0 ");
            }

            Debug.WriteLine("");
        }

        

    }
}
