using System;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CA1416
#pragma warning disable IDE0044

/// <summary>
/// An attempt at making a simple Tetris clone, implemented in C# and built using .NET 5.
/// Originally, I was writing this in C++, but switched to C# after a while since I can't read C++ almost at all.
/// 
/// This project is directly based on this video: https://youtu.be/8OK8_tHeCIA
/// This code, and the software it produces, are licensed under the GNU General Public License, version 3.
/// Original license document: https://www.gnu.org/licenses/gpl-3.0.txt
/// MeowcatSoftware Digital Solutions (C) 2021
/// </summary>
namespace MeowcatSoftware.Experiments.TetrisDotNet
{
    internal class Program
    {
        const int FieldWidth = 12;
        const int FieldHeight = 18;
        const int ScreenWidth = 80;
        const int ScreenHeight = 30;

        static string[] Tetronimo = new string[7]; // Stores every tetronimo used by the game
        static char[] PlayField = new char[FieldHeight * FieldWidth]; // Acts as a buffer for rendering the play field
        static char[] Screen = new char[30 * 80]; // Acts as a buffer for the entire frame

        /// <summary>
        /// Rotates a tetronimo during gameplay.
        /// </summary>
        /// <param name="px">The current piece's X position inside the grid.</param>
        /// <param name="py">The current piece's Y position inside the grid.</param>
        /// <param name="r">The degree to which the piece should be rotated. (0 = 0, 1 = 90, 2 = 180, 3 = 270}</param>
        /// <returns>A magic number that somehow rotates the current tetronimo.</returns>
        static int Rotate(int px, int py, int r)
        {
            // Uses some weird magic number stuff that involves advanced mathematics.
            return (r % 4) switch
            {
                0 => py * 4 + px,
                1 => 12 + py - (px * 4),
                2 => 15 - (py * 4) - px,
                3 => 3 - py + (px * 4),
                _ => 0,
            };
        }

        /// <summary>
        /// Determines if a tetronimo will fit in the specified location
        /// </summary>
        /// <param name="nTetromino">The ID of the current piece.</param>
        /// <param name="nRotation">The rotation factor of the current piece.</param>
        /// <param name="nPosX">The target X position of the current piece.</param>
        /// <param name="nPosY">The target Y position of the current piece.</param>
        /// <returns>True if the piece will fit i nthe target location, false if not.</returns>
        static bool DoesPieceFit(int nTetromino, int nRotation, int nPosX, int nPosY)
        {
            // Iterating accross all 16 spaces that a tetronimo occupies...
            for (int px = 0; px < 4; px++)
                for (int py = 0; py < 4; py++)
                {
                    // ..we gather some numbers based on the parameters...
                    int pIndex = Rotate(px, py, nRotation);
                    int fIndex = (nPosY + py) * FieldWidth + (nPosX + px);

                    // ..and then make sure that the current piece won't collide with its target.
                    if (nPosX + px >= 0 && nPosX + px < FieldWidth)
                    {
                        if (nPosY + py >= 0 && nPosY + py < FieldHeight)
                        {
                            // Return false if the current piece can't go where it's trying to go.
                            if (Tetronimo[nTetromino][pIndex] == 'X' && PlayField[fIndex] != 0) return false;
                        }
                    }
                }
            // Return true if it's ok to head to its target.
            return true;
        }

        /// <summary>
        /// Self explainatory. *shrug*
        /// </summary>
        private static void Main()
        {
            // Set up the console window
            Console.Clear();
            Console.Title = "Tetris.NET - A Simplistic Tetris Clone Built On .NET 5";
            Console.WindowHeight = 30;
            Console.WindowWidth = 80;

            // Draw the title screen
            Console.SetCursorPosition(35, 13);
            Console.WriteLine("Tetris.NET");
            Console.SetCursorPosition(10, 14);
            Console.Write("An object stacking game based on \"Tetris\" by Alexey Pajitnov");
            Console.SetCursorPosition(29, 16);
            Console.WriteLine("Press [ETNER] to play");
            Console.SetCursorPosition(0, 28);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("MeowcatSoftware Digital Solutions ");
            Console.ResetColor();
            Console.WriteLine("(C) 2021");
            Console.WriteLine("This is free software, as defined by the FSF, under the GNU GPL v3");
            _ = Console.ReadLine();
            
            // Go to the next menu
            Console.Beep();

            // Keep in mind that all of these are actually 4x4 sprites when seen in-game.
            Tetronimo[0] = "..X...X...X...X.";
            Tetronimo[1] = "..X..XX...X.....";
            Tetronimo[2] = ".....XX..XX.....";
            Tetronimo[3] = "..X..XX..X......";
            Tetronimo[4] = ".X...XX...X.....";
            Tetronimo[5] = ".X...XX...X.....";
            Tetronimo[6] = "..X...X..XX.....";

            ClassicGame();

            Console.SetCursorPosition(0, 21);
            Console.WriteLine("== [ GAME OVER ] ==");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            _ = Console.ReadKey(true);
        }

        /// <summary>
        /// A game of Tetris, going by classic marathon rules.
        /// </summary>
        internal static void ClassicGame()
        {
            // Draws the initial play field.
            for (int x = 0; x < FieldWidth; x++)
                for (int y = 0; y < FieldHeight; y++)
                    PlayField[(y * FieldWidth) + x] = (char)((x == 0 || x == FieldWidth - 1 || y == FieldHeight - 1) ? 9 : 0);

            Random RNG = new();         // RNG for grabbing a random tetronimo when needed
            bool STOP = false;          // The master kill switch for the game session
            bool ForcePieceDown;        // Determines if the current piece should fall next tick
            ConsoleKeyInfo KeyDown;     // Helps with user input
            int CurrentPiece = 0;       // The numerical ID of the currently used tetronimo
            int CurrentRotation = 0;    // The rotation factor of the current piece
            int CurrentX = 0;           // The current X position of the current piece
            int CurrentY = 0;           // The current Y position of the current piece
            int CurrentSpeed = 20;      // How many ticks should pass before making the current piece fall down
            int TickCounter = 0;        // Counts the ticks since the last drop cycle
            List<int> Lines = new();    // Stores completed lines created by well-placed pieces
            Lines.Clear();

            // The main game loop
            while (!STOP)
            {
                // Current tick length is 25ms + the length of 1 game cycle
                Thread.Sleep(25);
                TickCounter++;
                Console.Clear(); // Gotta clear the terminal or we'll get scanlines.
                ForcePieceDown = (TickCounter == CurrentSpeed);

                // Nifty check for if a key is being pressed
                if (Console.KeyAvailable)
                {
                    KeyDown = Console.ReadKey(true);
                    switch (KeyDown.Key)
                    {
                        case ConsoleKey.Escape: // [Esc] immediately terminates the session
                            STOP = true;
                            break;
                        case ConsoleKey.LeftArrow: // [Left] and [Right] are pretty obvious
                            if (DoesPieceFit(CurrentPiece, CurrentRotation, CurrentX - 1, CurrentY)) CurrentX--;
                            break;
                        case ConsoleKey.RightArrow:
                            if (DoesPieceFit(CurrentPiece, CurrentRotation, CurrentX + 1, CurrentY)) CurrentX++;
                            break;
                        case ConsoleKey.DownArrow: // [Down] drops the current piece
                            if (DoesPieceFit(CurrentPiece, CurrentRotation, CurrentX, CurrentY + 1)) CurrentY++;
                            break;
                        case ConsoleKey.R: // [R] rotates the current piece
                            if (DoesPieceFit(CurrentPiece, CurrentRotation + 1, CurrentX, CurrentY)) CurrentRotation++;
                            break;
                        default: // If any other keys are pressed, nothing happens.
                            break;
                    }
                }

                // If the game is executing an automatic drop
                if (ForcePieceDown)
                {
                    // Let's make sure that the piece fits where it's going.
                    if (DoesPieceFit(CurrentPiece, CurrentRotation, CurrentX, CurrentY + 1))
                    { 
                        CurrentY++;
                        ForcePieceDown = false;
                        TickCounter = 0;
                    }
                    else
                    {
                        // End the game if the piece can't be locked into the field.
                        if (!DoesPieceFit(CurrentPiece, CurrentRotation, CurrentX, CurrentY))
                        {
                            STOP = true;
                        }
                        else // If the game's not over, lock the piece in and bring in another tetronimo
                        {
                            // Commit the tetromino to its resting place
                            for (int px = 0; px < 4; px++)
                                for (int py = 0; py < 4; py++)
                                    if (Tetronimo[CurrentPiece][Rotate(px, py, CurrentRotation)] == 'X')
                                        PlayField[(CurrentY + py) * FieldWidth + (CurrentX + px)] = (char)(CurrentPiece + 1);

                            // Scan for full lines, alerting the player if any are found.
                            for (int py = 0; py < 4; py++)
                                if (CurrentY + py < FieldHeight)
                                {
                                    // The line is assumed to be complete unless proven otherwise.
                                    bool IsFullLine = true;
                                    for (int px = 1; px < FieldWidth; px++)
                                        IsFullLine &= (PlayField[(CurrentY + py) * FieldWidth + px]) != 0;

                                    // If a line passed the sanity check, it must be full. Well done, user!
                                    if (IsFullLine)
                                    {
                                        // Highlight the completed line and add it to the list
                                        for (int px = 1; px < FieldWidth - 1; px++)
                                            PlayField[(CurrentY + py) * FieldWidth + px] = (char)8;

                                        Lines.Add(CurrentY + py);
                                    }
                                }

                            // The logic is reset and a new piece is spawned.
                            CurrentX = 0;
                            CurrentY = 0;
                            CurrentRotation = 0;
                            CurrentPiece = RNG.Next(1, 8);
                            TickCounter = 0;
                        }
                    }
                }

                // Renders the play field and anything in it
                for (int x = 0; x < FieldWidth; x++)
                    for (int y = 0; y < FieldHeight; y++)
                        Screen[(y + 2) * ScreenWidth + (x + 2)] = " ABCDEFG=#"[PlayField[y * FieldWidth + x]];

                // Acts as a screen buffer
                for (int px = 0; px < 4; px++)
                    for (int py = 0; py < 4; py++)
                        if (Tetronimo[CurrentPiece][Rotate(px, py, CurrentRotation)] == 'X')
                            Screen[(CurrentY + py + 2) * ScreenWidth + (CurrentX + px + 2)] = (char)(CurrentPiece + 65);

                // Detects if a complete line was found earlier
                if (!(Lines.Count == 0))
                {
                    // Immediately show the current frame and pause so the user notices
                    Console.Write(Screen);
                    Thread.Sleep(500);

                    // Then delete the completed line and drop everything else
                    foreach (int Line in Lines)
                    {
                        for (int px = 1; px < FieldWidth - 1; px++)
                        {
                            for (int py = Line; py > 0; py--)
                                PlayField[py * FieldWidth + px] = PlayField[(py - 1) * FieldWidth + px];
                            PlayField[px] = (char)0;
                            Lines.RemoveAt(0); // < Line must be removed from the list, but exception occurs every time?
                        }
                    }
                }

                // Write the buffer to the terminal window.
                Console.Write(Screen);
            }
            return;
        }
    }
}