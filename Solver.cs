using System;
using System.Collections.Generic;

namespace MinesweeperSolver
{
    class Solver
    {
        private int row;
        private int col;
        private int mine;
        private int[,] board;
        private int mineLeft;
        private int[,] maxMineCount;
        private HashSet<Tuple<int, int>> flaggingPoints;
        private HashSet<Tuple<int, int>> diggingPoints;
        private HashSet<Tuple<int, int>> suspiciousPoints;
        private List<List<Tuple<int, int>>> forest;
        private HashSet<Tuple<int, int>> visited;
        private List<Tuple<Tuple<int, int>, double>> possibility;
        private Random random;
        private DateTime endTime;
        private bool searchFinished;
        private static Tuple<int, int>[] around = { Tuple.Create( -1, -1 ), Tuple.Create( -1, 0 ), Tuple.Create( -1, 1 ), Tuple.Create( 0, -1 ),
                                                    Tuple.Create( 0, 1 ), Tuple.Create( 1, -1 ), Tuple.Create( 1, 0 ), Tuple.Create( 1, 1 ) };
        private static Tuple<int, int>[] border = { Tuple.Create(-1, 0), Tuple.Create(0, -1), Tuple.Create(0, 1), Tuple.Create(1, 0) };

        public Solver(int row, int col, int mine)
        {
            this.row = row;
            this.col = col;
            this.mine = mine;
            mineLeft = mine;
            random = new Random();
            board = new int[row, col];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    board[i, j] = -2;
                }
            }
        }

        public void SetBlock(int row, int col, int val)
        {
            board[row, col] = val;
        }

        public int GetBlock(int row, int col)
        {
            return board[row, col];
        }

        public bool isSolved()
        {
            return (mineLeft == 0);
        }

        public HashSet<Tuple<int, int>> GetFlaggingPoints()
        {
            return flaggingPoints;
        }

        public HashSet<Tuple<int, int>> GetDiggingPoints()
        {
            return diggingPoints;
        }

        void FillTree(List<Tuple<int, int>> tree)
        {
            int totalCount = 0;
            var successCount = new Dictionary<Tuple<int, int>, int>();
            foreach (var point in tree)
            {
                if (suspiciousPoints.Contains(point))
                {
                    successCount.Add(point, 0);
                }
            }
            endTime = DateTime.Now.AddSeconds(5);
            searchFinished = true;
            TryFillTree(ref tree, 0, ref successCount, ref totalCount);
            foreach (var entry in successCount)
            {
                if (searchFinished && entry.Value == 0)
                {
                    diggingPoints.Add(entry.Key);
                }
                else if (searchFinished && entry.Value == totalCount)
                {
                    Tuple<int, int> point = entry.Key;
                    board[point.Item1, point.Item2] = -1;
                    mineLeft--;
                    flaggingPoints.Add(point);
                }
                else
                {
                    possibility.Add(Tuple.Create(entry.Key, (double)entry.Value / totalCount));
                }
            }
        }

        bool CheckBlock(Tuple<int, int> p)
        {
            foreach (var delta in around)
            {
                int x = p.Item1 + delta.Item1;
                int y = p.Item2 + delta.Item2;
                if (CheckRange(x, y) && board[x, y] > 0 && CountAround(x, y, -2) == 0)
                {
                    if (CountAround(x, y, -1) != board[x, y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        void TryFillTree(ref List<Tuple<int, int>> tree, int index, ref Dictionary<Tuple<int, int>, int> successCount, ref int totalCount)
        {
            if (index != 0 && (!CheckBlock(tree[index - 1])))
            {
                return;
            }
            if (index == tree.Count)
            {
                foreach (var p in tree)
                {
                    int x = p.Item1;
                    int y = p.Item2;
                    if (board[x, y] == -1)
                    {
                        successCount[p]++;
                    }
                }
                totalCount++;
                return;
            }
            if (DateTime.Now > endTime)
            {
                searchFinished = false;
                return;
            }
            if (mineLeft > 0)
            {
                int x = tree[index].Item1;
                int y = tree[index].Item2;
                board[x, y] = -1;
                mineLeft--;
                TryFillTree(ref tree, index + 1, ref successCount, ref totalCount);
                board[x, y] = -2;
                mineLeft++;
            }
            TryFillTree(ref tree, index + 1, ref successCount, ref totalCount);
        }

        void FindTrees()
        {
            forest = new List<List<Tuple<int, int>>>();
            visited = new HashSet<Tuple<int, int>>();
            possibility = new List<Tuple<Tuple<int, int>, double>>();
            foreach (Tuple<int, int> p in suspiciousPoints)
            {
                if (visited.Contains(p)) continue;
                var tree = new List<Tuple<int, int>>();
                DFSFindTrees(p, ref tree);
                forest.Add(tree);
            }
            forest.Sort(delegate (List<Tuple<int, int>> t1, List<Tuple<int, int>> t2)
            {
                return t1.Count.CompareTo(t2.Count);
            });
        }

        void DFSFindTrees(Tuple<int, int> p, ref List<Tuple<int, int>> tree)
        {
            visited.Add(p);
            if (suspiciousPoints.Contains(p))
            {
                tree.Add(p);
            }
            foreach (var delta in border)
            {
                int x = p.Item1 + delta.Item1;
                int y = p.Item2 + delta.Item2;
                if (!CheckRange(x, y)) continue;
                Tuple<int, int> p2 = Tuple.Create(x, y);
                if (visited.Contains(p2)) continue;
                if ((board[x, y] > 0 && CountAround(x, y, -2) > 0) || suspiciousPoints.Contains(p2))
                {
                    DFSFindTrees(p2, ref tree);
                }
            }
        }

        int CountAround(int x, int y, int val)
        {
            int count = 0;
            foreach (Tuple<int, int> delta in around)
            {
                int i = x + delta.Item1;
                int j = y + delta.Item2;
                if (CheckRange(i, j) && board[i, j] == val)
                {
                    count++;
                }
            }
            return count;
        }

        bool CheckRange(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < row && y < col);
        }

        public void Search()
        {
            flaggingPoints = new HashSet<Tuple<int, int>>();
            diggingPoints = new HashSet<Tuple<int, int>>();
            MakeMaxMineCount();
            if (flaggingPoints.Count == 0 && diggingPoints.Count == 0)
            {
                FindTrees();
                foreach (var tree in forest)
                {
                    FillTree(tree);
                }
                if (diggingPoints.Count == 0 && flaggingPoints.Count == 0)
                {
                    List<Tuple<int, int>> unknownBlocks = new List<Tuple<int, int>>();
                    for (int i = 0; i < row; i++)
                    {
                        for (int j = 0; j < col; j++)
                        {
                            var p = Tuple.Create(i, j);
                            if (board[i, j] == -2 && (!suspiciousPoints.Contains(p)))
                            {
                                unknownBlocks.Add(p);
                            }
                        }
                    }
                    possibility.Sort(delegate (Tuple<Tuple<int, int>, double> p1, Tuple<Tuple<int, int>, double> p2)
                    {
                        return p1.Item2.CompareTo(p2.Item2);
                    });
                    int unknownCount = unknownBlocks.Count;
                    double minSuspiciousPossibility = possibility.Count == 0 ? 1 : possibility[0].Item2;
                    if (minSuspiciousPossibility * minSuspiciousPossibility > ((double)mineLeft / unknownCount))
                    {
                        diggingPoints.Add(unknownBlocks[random.Next(unknownCount)]);
                    }
                    else
                    {
                        List<Tuple<int, int>> pointsWithMinimumPossibility = new List<Tuple<int, int>>();
                        foreach (var p in possibility)
                        {
                            if (p.Item2 > minSuspiciousPossibility) break;
                            pointsWithMinimumPossibility.Add(p.Item1);
                        }
                        diggingPoints.Add(pointsWithMinimumPossibility[random.Next(pointsWithMinimumPossibility.Count)]);
                    }
                }
            }
        }

        void MakeMaxMineCount()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                suspiciousPoints = new HashSet<Tuple<int, int>>();
                maxMineCount = new int[row, col];
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        if (board[i, j] > 0)
                        {
                            int count = board[i, j] - CountAround(i, j, -1);
                            if (count == 0)
                            {
                                foreach (Tuple<int, int> delta in around)
                                {
                                    int x = i + delta.Item1;
                                    int y = j + delta.Item2;
                                    if (CheckRange(x, y) && board[x, y] == -2)
                                    {
                                        diggingPoints.Add(Tuple.Create(x, y));
                                    }
                                }
                            }
                            else if (count == CountAround(i, j, -2))
                            {
                                foreach (Tuple<int, int> delta in around)
                                {
                                    int x = i + delta.Item1;
                                    int y = j + delta.Item2;
                                    if (CheckRange(x, y) && board[x, y] == -2)
                                    {
                                        board[x, y] = -1;
                                        mineLeft--;
                                        flaggingPoints.Add(Tuple.Create(x, y));
                                        changed = true;
                                    }
                                }
                            }
                            else
                            {
                                foreach (Tuple<int, int> delta in around)
                                {
                                    int x = i + delta.Item1;
                                    int y = j + delta.Item2;
                                    if (CheckRange(x, y) && board[x, y] == -2)
                                    {
                                        maxMineCount[x, y] = count;
                                        suspiciousPoints.Add(Tuple.Create(x, y));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
