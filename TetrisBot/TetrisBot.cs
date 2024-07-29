using System;
using System.Collections.Generic;
using System.Linq;

// Basic Tetris game
public class Tetris
{
    private const int Width = 10;
    private const int Height = 20;
    private static readonly int[][,] Tetrominoes = new int[][,]
    {
        new int[,] { {1,1,1,1} },           // I
        new int[,] { {1,1}, {1,1} },        // O
        new int[,] { {0,1,0}, {1,1,1} },    // T
        new int[,] { {0,0,1}, {1,1,1} },    // L
        new int[,] { {1,0,0}, {1,1,1} },    // J
        new int[,] { {0,1,1}, {1,1,0} },    // S
        new int[,] { {1,1,0}, {0,1,1} }     // Z
    };

    public int[,] Board { get; private set; }
    public int Score { get; private set; }
    public bool GameOver { get; private set; }

    private int[,] currentPiece;
    private int currentX;
    private int currentY;
    private Random random;

    public Tetris()
    {
        Board = new int[Height, Width];
        Score = 0;
        GameOver = false;
        random = new Random();
        SpawnNewPiece();
    }

    private void SpawnNewPiece()
    {
        currentPiece = Tetrominoes[random.Next(Tetrominoes.Length)];
        currentX = Width / 2 - currentPiece.GetLength(1) / 2;
        currentY = 0;

        if (!IsValidMove(currentX, currentY, currentPiece))
        {
            GameOver = true;
        }
    }

    public void Step(int action)
    {
        switch (action)
        {
            case 0: // Move left
                if (IsValidMove(currentX - 1, currentY, currentPiece))
                    currentX--;
                break;
            case 1: // Move right
                if (IsValidMove(currentX + 1, currentY, currentPiece))
                    currentX++;
                break;
            case 2: // Rotate
                var rotated = RotatePiece(currentPiece);
                if (IsValidMove(currentX, currentY, rotated))
                    currentPiece = rotated;
                break;
            case 3: // Drop
                while (IsValidMove(currentX, currentY + 1, currentPiece))
                    currentY++;
                PlacePiece();
                break;
        }

        // Always try to move down after each action
        if (IsValidMove(currentX, currentY + 1, currentPiece))
        {
            currentY++;
        }
        else
        {
            PlacePiece();
        }
    }

    private bool IsValidMove(int x, int y, int[,] piece)
    {
        for (int i = 0; i < piece.GetLength(0); i++)
        {
            for (int j = 0; j < piece.GetLength(1); j++)
            {
                if (piece[i, j] != 0)
                {
                    if (x + j < 0 || x + j >= Width || y + i >= Height)
                        return false;
                    if (y + i >= 0 && Board[y + i, x + j] != 0)
                        return false;
                }
            }
        }
        return true;
    }

    private void PlacePiece()
    {
        for (int i = 0; i < currentPiece.GetLength(0); i++)
        {
            for (int j = 0; j < currentPiece.GetLength(1); j++)
            {
                if (currentPiece[i, j] != 0)
                {
                    Board[currentY + i, currentX + j] = currentPiece[i, j];
                }
            }
        }

        ClearLines();
        SpawnNewPiece();
    }

    private void ClearLines()
    {
        for (int i = Height - 1; i >= 0; i--)
        {
            if (IsLineFull(i))
            {
                ClearLine(i);
                ShiftLinesDown(i);
                i++; // Recheck the same line
                Score += 100;
            }
        }
    }

    private bool IsLineFull(int y)
    {
        for (int x = 0; x < Width; x++)
        {
            if (Board[y, x] == 0)
                return false;
        }
        return true;
    }

    private void ClearLine(int y)
    {
        for (int x = 0; x < Width; x++)
        {
            Board[y, x] = 0;
        }
    }

    private void ShiftLinesDown(int clearedLine)
    {
        for (int y = clearedLine - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                Board[y + 1, x] = Board[y, x];
            }
        }
    }

    private int[,] RotatePiece(int[,] piece)
    {
        int n = piece.GetLength(0);
        int m = piece.GetLength(1);
        int[,] rotated = new int[m, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                rotated[j, n - 1 - i] = piece[i, j];
            }
        }
        return rotated;
    }
}

// Q-learning agent
public class TetrisAgent
{
    private Dictionary<string, Dictionary<int, double>> QTable;
    private Random random = new Random();
    private double epsilon = 0.1;
    private double alpha = 0.1;
    private double gamma = 0.99;

    public TetrisAgent()
    {
        QTable = new Dictionary<string, Dictionary<int, double>>();
    }

    public string GetState(Tetris game)
    {
        var board = game.Board;
        var heights = Enumerable.Range(0, board.GetLength(1))
            .Select(col => Enumerable.Range(0, board.GetLength(0))
                .Count(row => board[row, col] != 0))
            .ToArray();

        var holes = Enumerable.Range(0, board.GetLength(1))
            .Select(col => Enumerable.Range(0, heights[col])
                .Count(row => board[row, col] == 0))
            .Sum();

        var bumpiness = Enumerable.Range(0, board.GetLength(1) - 1)
            .Select(i => Math.Abs(heights[i] - heights[i + 1]))
            .Sum();

        return string.Join(",", heights) + "|" + holes + "|" + bumpiness;
    }

    public int GetAction(Tetris game)
    {
        string state = GetState(game);

        if (!QTable.ContainsKey(state))
        {
            QTable[state] = new Dictionary<int, double>
            {
                {0, 0}, {1, 0}, {2, 0}, {3, 0}
            };
        }

        if (random.NextDouble() < epsilon)
        {
            return random.Next(4);
        }
        else
        {
            return QTable[state].OrderByDescending(x => x.Value).First().Key;
        }
    }

    public void UpdateQ(string state, int action, double reward, string nextState)
    {
        if (!QTable.ContainsKey(nextState))
        {
            QTable[nextState] = new Dictionary<int, double>
            {
                {0, 0}, {1, 0}, {2, 0}, {3, 0}
            };
        }

        double maxNextQ = QTable[nextState].Values.Max();
        QTable[state][action] += alpha * (reward + gamma * maxNextQ - QTable[state][action]);
    }
}

// Training loop
public class Trainer
{
    public void Train(int episodes)
    {
        var agent = new TetrisAgent();

        for (int episode = 0; episode < episodes; episode++)
        {
            var game = new Tetris();
            string state = agent.GetState(game);
            int totalReward = 0;

            while (!game.GameOver)
            {
                int action = agent.GetAction(game);
                int prevScore = game.Score;
                int prevHeight = GetTotalHeight(game.Board);

                game.Step(action);

                string nextState = agent.GetState(game);
                int newHeight = GetTotalHeight(game.Board);
                double reward = CalculateReward(game, prevScore, prevHeight, newHeight);
                totalReward += (int)reward;

                agent.UpdateQ(state, action, reward, nextState);
                state = nextState;
            }

            if (episode % 100 == 0)
            {
                Console.WriteLine($"Episode {episode}, Score: {game.Score}, Total Reward: {totalReward}");
            }
        }
    }

    private int GetTotalHeight(int[,] board)
    {
        return Enumerable.Range(0, board.GetLength(1))
            .Select(col => Enumerable.Range(0, board.GetLength(0))
                .Count(row => board[row, col] != 0))
            .Sum();
    }

    private double CalculateReward(Tetris game, int prevScore, int prevHeight, int newHeight)
    {
        double reward = 0;

        // Reward for clearing lines
        reward += (game.Score - prevScore);

        // Penalize increase in total height
        reward -= (newHeight - prevHeight) * 0.1;

        // Heavy penalty for game over
        if (game.GameOver)
            reward -= 500;

        return reward;
    }
}

// Main program
class TetrisBot
{
    static void Main(string[] args)
    {
        var trainer = new Trainer();
        trainer.Train(10000);  // Train for 10,000 episodes

        Console.WriteLine("Training completed. You can now use the trained agent to play Tetris.");
    }
}