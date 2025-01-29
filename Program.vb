Option Strict On
Option Infer On
Imports System.Diagnostics.CodeAnalysis

Public Module Program
    Friend playerPos As New GridIndex(17, 10), pressedKey As New ConsoleKeyInfo
    Friend ReadOnly fruitTimer As New Stopwatch, scaredTimer As New Stopwatch

    Friend ReadOnly Property InitialEnemyInfo As (pos As GridIndex, scared As Boolean)() =
        New(pos As GridIndex, scared As Boolean)(3) {
            (New GridIndex(10, 10), False), (New GridIndex(11, 9), False),
            (New GridIndex(11, 10), False), (New GridIndex(11, 11), False)
        }

    Friend ReadOnly Property InitialGameMap As Char(,)
        Get
            Dim mapContent As String() = New String(20) {
                "########## ############",
                "#.*....### ####..*#...#",
                "#.##.#.### ####.#.#.#.#",
                "#.##.#.### ####.#...#.#",
                "#.##.#.### ####.###.#.#",
                "#...................#.#",
                "#.##.##### ####.#.###.#",
                "#.##...#       .#...#.#",
                "#.##.#.# #### #.#.#.#.#",
                "#....#.  #  # #...#...#",
                "####.###    # ### ###.#",
                "#....#.  #  # #...#...#",
                "#.##.#.# #### #.#.#.#.#",
                "#.##...#       .#...#.#",
                "#.##.##### ####.#.###.#",
                "#...................#.#",
                "#.##.#.### ####.###.#.#",
                "#.##.#.### ####.#...#.#",
                "#.##.#.### ####.#.#.#.#",
                "#.*....### ####..*#...#",
                "########## ############"
            }

            Dim result(MaxLineIndex(mapContent), UBound(mapContent)) As Char

            For x As Integer = 0 To UBound(result, 1) Step 1
                For y As Integer = 0 To UBound(result, 2) Step 1
                    result(x, y) = mapContent(y)(x)
                Next y
            Next x

            Return result
        End Get
    End Property

    Private isGameInProcess As Boolean = False, isPowerPelletEaten As Boolean = False

    Friend Sub Main()
        Console.Clear()  ' Clear the terminal for the title screen display.
        Console.CursorVisible = False
        Randomize()
        
        Dim currGameMap As Char(,) = CType(InitialGameMap.Clone(), Char(,))
        Dim currEnemyInfo(3) As (pos As GridIndex, scared As Boolean)
        Array.Copy(InitialEnemyInfo, currEnemyInfo, InitialEnemyInfo.Length)
        Dim playerScore As New Integer, playerLives As Integer = 3, currLevel As Integer = 1

        Dim GetRandomTarget = Function() As GridIndex
                                  Dim rndX = CInt(Rnd(UBound(InitialGameMap, 1)))
                                  Dim rndY = CInt(Rnd(UBound(InitialGameMap, 2)))
                                  Return New GridIndex(rndX, rndY)
                              End Function

        ' This part of code handles the fixed update during the gameplay.
        Task.Run(Sub()
                     Do
                         pressedKey = Console.ReadKey()
                         If Not isGameInProcess Then Continue Do
                         Dim enemyDirections(3) As GridIndex, targetPos As GridIndex
                         For i As Integer = 0 To UBound(currEnemyInfo) Step 1
                             Select Case i
                                 Case 0
                                     targetPos = playerPos
                                 Case 1
                                     targetPos = playerPos - PlayerDirection * 4
                                 Case 2
                                     targetPos = currEnemyInfo(0).pos * 2 - playerPos
                                 Case 3
                                     targetPos = GetRandomTarget()
                             End Select
                             Dim ghostRoute = AStarAlgorithm.FindUniqueRoute(WalkableTerrain,
                                    currEnemyInfo(i).pos + enemyDirections(i), targetPos)

                             If ghostRoute.Count > 0 Then enemyDirections(i) = ghostRoute(0)
                             currEnemyInfo(i).pos += enemyDirections(i)
                             If isPowerPelletEaten Then currEnemyInfo(i).scared = True
                             If Not scaredTimer.IsRunning Then currEnemyInfo(i).scared = False
                         Next i
                         If currEnemyInfo.All(Function(enemy) enemy.scared) Then
                             isPowerPelletEaten = False
                         End If
                     Loop
                 End Sub)

        Do
            If isGameInProcess Then
                GameplayProcess(currGameMap, playerScore, playerLives, currLevel, currEnemyInfo)
            Else
                DisplayTitleScreen()
            End If
        Loop
    End Sub

    <Runtime.CompilerServices.Extension> Friend Sub StopAndReset(stopwatch As Stopwatch)
        ' The process of stoping and resetting the in-game timers will be condensed to
        ' this unique extension method.
        With stopwatch : .Stop() : .Reset() : End With
    End Sub

    Friend ReadOnly Property WalkableTerrain As Boolean(,)
        Get
            Dim maxRowIdx = UBound(InitialGameMap, 1), maxColIdx = UBound(InitialGameMap, 2)
            Dim terrain(maxRowIdx, maxColIdx) As Boolean
            For i As Integer = 0 To maxRowIdx Step 1
                For j As Integer = 0 To maxColIdx Step 1
                    If InitialGameMap(i, j) <> "#"c Then terrain(i, j) = True
                Next j
            Next i
            Return terrain
        End Get
    End Property

    Private ReadOnly Property TimerThreshold(level As Integer, isForGhost As Boolean) As Double
        Get
            If isForGhost Then
                Return If(level < 5, 10 - level * 1.5, 5) * 1000
            Else
                Return If(level < 5, 8 + level * 1.5, 15) * 1000
            End If
        End Get
    End Property

    Private Sub DisplayTitleScreen()
        Console.ForegroundColor = ConsoleColor.White
        Console.SetCursorPosition(7, 5)
        Console.Write("-* TERMINAL PAC-MAN GAME *-")
        Console.SetCursorPosition(5, 7)
        Console.Write("Press ""Enter"" to begin the game.")
        If pressedKey.Key = ConsoleKey.Enter Then isGameInProcess = True
    End Sub

    Private Sub GameplayProcess(ByRef gameMap As Char(,), ByRef score%, ByRef lives%,
             ByRef level%, ByRef enemyInfo As (pos As GridIndex, scared As Boolean)())

        Static bonusMult As New Integer, playerHasExtraLife As Boolean = False
        Console.Clear()

        Dim direction As GridIndex = PlayerDirection
        Dim nextPosX As Integer = playerPos.x + direction.x
        Dim nextPosY As Integer = playerPos.y + direction.y

        If nextPosX > UBound(gameMap, 1) Then nextPosX = 0
        If nextPosX < 0 Then nextPosX = UBound(gameMap, 1)
        If nextPosY > UBound(gameMap, 2) Then nextPosY = 0
        If nextPosY < 0 Then nextPosY = UBound(gameMap, 2)

        If gameMap(nextPosX, nextPosY) <> "#" Then
            playerPos = New GridIndex(nextPosX, nextPosY)

            Select Case gameMap(nextPosX, nextPosY)
                Case "."c
                    score += 10
                    If Not fruitTimer.IsRunning Then fruitTimer.Start()
                Case "*"c
                    isPowerPelletEaten = True
                    score += 50
                    With scaredTimer
                        Call If(.IsRunning, Sub() .Restart(), Sub() .Start())
                    End With
                Case "%"c
                    score += 100 + level * 200
                    fruitTimer.StopAndReset()
            End Select

            gameMap(playerPos.x, playerPos.y) = " "c
        End If

        ' This part of code displays the game map (i.e. the play area) on the screen.
        For y As Integer = 0 To UBound(gameMap, 2) Step 1
            For x As Integer = 0 To UBound(gameMap, 1) Step 1
                Select Case gameMap(x, y)
                    Case "#"c
                        Console.ForegroundColor = ConsoleColor.Blue
                    Case "*"c
                        Console.ForegroundColor = ConsoleColor.Cyan
                    Case "%"c
                        Console.ForegroundColor = ConsoleColor.Magenta
                    Case "."c
                        Console.ForegroundColor = ConsoleColor.White
                End Select
                Console.Write(gameMap(x, y))
            Next x
            Console.WriteLine()
        Next y

        Dim isCaughtByGhost As Boolean = False
        For Each enemy As (pos As GridIndex, scared As Boolean) In enemyInfo
            Console.ForegroundColor = If(enemy.scared, ConsoleColor.Cyan, ConsoleColor.Red)
            Console.SetCursorPosition(enemy.pos.x, enemy.pos.y)
            Console.Write("&"c)
            If Not enemy.scared AndAlso enemy.pos = playerPos Then isCaughtByGhost = True
        Next enemy
        If isCaughtByGhost Then
            lives -= 1
            fruitTimer.StopAndReset()
            gameMap(13, 10) = " "c
            playerPos = New GridIndex(17, 10)
            Array.Copy(InitialEnemyInfo, enemyInfo, InitialEnemyInfo.Length)
        End If

        With scaredTimer
            If .IsRunning Then
                Dim millisecLeft# = TimerThreshold(level, True) - .ElapsedMilliseconds

                For i As Integer = 0 To UBound(enemyInfo) Step 1
                    If playerPos = enemyInfo(i).pos Then
                        enemyInfo(i).pos = InitialEnemyInfo(i).pos
                        enemyInfo(i).scared = False
                        bonusMult += 1
                        score += CInt(2 ^ bonusMult * 100)
                    End If
                    If millisecLeft <= 0 Then
                        bonusMult = 0
                        .StopAndReset()
                        Exit For
                    End If
                Next i
                If millisecLeft > 0 Then
                    Console.ForegroundColor = ConsoleColor.Cyan
                    Console.SetCursorPosition(25, 5)
                    Console.Write($"Power-up: {Math.Floor(millisecLeft / 1000)} sec. left")
                End If
            End If
        End With

        With fruitTimer
            If .IsRunning AndAlso .ElapsedMilliseconds > TimerThreshold(level, False) Then
                gameMap(13, 10) = "%"c
                Dim interval As Integer = 15000 - (level - 1) * 2000
                If .ElapsedMilliseconds > TimerThreshold(level, False) + interval Then
                    gameMap(13, 10) = " "c
                    .StopAndReset()
                End If
            End If
        End With

        Dim hasPellet As Boolean = False
        For x As Integer = 0 To UBound(gameMap, 1) Step 1
            For y As Integer = 0 To UBound(gameMap, 2) Step 1
                If gameMap(x, y) = "."c OrElse gameMap(x, y) = "*"c Then hasPellet = True
            Next y
        Next x
        If Not hasPellet Then
            level += 1
            gameMap = CType(InitialGameMap.Clone(), Char(,))
            playerPos = New GridIndex(17, 10)
            Array.Copy(InitialEnemyInfo, enemyInfo, InitialEnemyInfo.Length)
            fruitTimer.StopAndReset()
            scaredTimer.StopAndReset()
        End If

        ' When the player loses all the lives, or completes all 7 levels, the game ends.
        ' The gameplay result will be displayed before going back to the title screen. 
        If lives <= 0 OrElse level > 7 Then
            gameMap = CType(InitialGameMap.Clone(), Char(,))
            fruitTimer.StopAndReset()
            scaredTimer.StopAndReset()
            isGameInProcess = False
            Console.Clear()
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.SetCursorPosition(5, 10)
            Dim prevLvlInfo As String = If(level < 7, $"Level {level}", "All Clear!")
            Console.Write($"Previous Score: {score,5} ({prevLvlInfo})")
            score = 0 : lives = 3 : level = 1
            Exit Sub
        End If

        If Not playerHasExtraLife AndAlso score > 10000 Then
            lives += 1
            playerHasExtraLife = True
        End If

        Console.ForegroundColor = ConsoleColor.Yellow
        Console.SetCursorPosition(playerPos.x, playerPos.y)
        Console.Write("@"c)
        Console.SetCursorPosition(25, 0)
        Console.Write($"Score: {score,5}")
        Console.SetCursorPosition(25, 1)
        Console.Write($"Lives: {New String("@"c, lives - 1)}")

        Console.ForegroundColor = ConsoleColor.Magenta
        Console.SetCursorPosition(25, 3)
        Console.Write(New String("%"c, level))

        Threading.Thread.Sleep(500 - level * 50)
    End Sub

    Private ReadOnly Property PlayerDirection As GridIndex
        Get
            Dim keyMapping As New Dictionary(Of ConsoleKey, GridIndex) From {
                {ConsoleKey.UpArrow, GridIndex.Up},
                {ConsoleKey.DownArrow, GridIndex.Down},
                {ConsoleKey.LeftArrow, GridIndex.Left},
                {ConsoleKey.RightArrow, GridIndex.Right}
            }
            Return keyMapping.GetValueOrDefault(pressedKey.Key)
        End Get
    End Property

    Private ReadOnly Property MaxLineIndex(lines As String()) As Integer
        Get
            Dim longestLine = Aggregate line As String In lines
                                  Order By line.Length Descending Into First()

            Return UBound(longestLine.ToCharArray())
        End Get
    End Property
End Module

Friend Structure GridIndex
    Public ReadOnly x As Integer, y As Integer

    Public Sub New(x As Integer, y As Integer)
        Me.x = x
        Me.y = y
    End Sub

    Public Overrides Function Equals(<NotNullWhen(True)> obj As Object) As Boolean
        Dim other As GridIndex = DirectCast(obj, GridIndex)
        Return x = other.x AndAlso y = other.y
    End Function

    Public Shared Operator =(left As GridIndex, right As GridIndex) As Boolean
        Return left.Equals(right)
    End Operator

    Public Shared Operator <>(left As GridIndex, right As GridIndex) As Boolean
        Return Not left.Equals(right)
    End Operator

    Public Shared Operator +(left As GridIndex, right As GridIndex) As GridIndex
        Return New GridIndex(left.x + right.x, left.y + right.y)
    End Operator

    Public Shared Operator -(left As GridIndex, right As GridIndex) As GridIndex
        Return New GridIndex(left.x - right.x, left.y - right.y)
    End Operator

    Public Shared Operator *(vec As GridIndex, scale As Integer) As GridIndex
        Return New GridIndex(vec.x - scale, vec.y - scale)
    End Operator

    Public Shared ReadOnly Property Up As New GridIndex(0, -1)
    Public Shared ReadOnly Property Down As New GridIndex(0, 1)
    Public Shared ReadOnly Property Left As New GridIndex(-1, 0)
    Public Shared ReadOnly Property Right As New GridIndex(1, 0)

    Public Overrides Function GetHashCode() As Integer
        Return HashCode.Combine(x, y)
    End Function
End Structure

Friend NotInheritable Class AStarAlgorithm
    Private ReadOnly gridIdx As GridIndex
    Private costF As Integer, costG As Integer, costH As Integer

    Private ReadOnly Property PreviousRef As AStarAlgorithm

    Private Shared ReadOnly StandardDirections As GridIndex() =
        New GridIndex(3) {GridIndex.Up, GridIndex.Down, GridIndex.Right, GridIndex.Left}

    Private Sub New(gridIdx As GridIndex, Optional prevRef As AStarAlgorithm = Nothing)
        Me.gridIdx = gridIdx
        PreviousRef = prevRef
    End Sub

    Public Overrides Function Equals(obj As Object) As Boolean
        Dim other As AStarAlgorithm = TryCast(obj, AStarAlgorithm)
        Return other IsNot Nothing AndAlso gridIdx = other.gridIdx
    End Function

    Public Overrides Function GetHashCode() As Integer
        Return gridIdx.GetHashCode()
    End Function

    Public Shared Function FindUniqueRoute(walkableTerrain As Boolean(,),
             startGridIdx As GridIndex, finishGridIdx As GridIndex) As List(Of GridIndex)
        ' Note: This function doesn't return specific grid indices of the result path,
        ' but indicates the movement from one grid index to the next.
        Dim startPoint As New AStarAlgorithm(startGridIdx)
        Dim finishPoint As New AStarAlgorithm(finishGridIdx)
        Dim openList As New List(Of AStarAlgorithm) From {startPoint}
        Dim closedList As New List(Of AStarAlgorithm)

        Dim SquaredMagnitude = Function(left As GridIndex, right As GridIndex) As Integer
                                   Dim sqrDiffX As Double = (left.x - right.x) ^ 2
                                   Dim sqrDiffY As Double = (left.y - right.y) ^ 2
                                   Return CInt(sqrDiffX + sqrDiffY)
                               End Function
        Dim CalcDotProduct = Function(left As GridIndex, right As GridIndex) As Integer
                                 Return left.x * right.x + left.y * right.y
                             End Function

        Do Until openList.Count = 0
            ' Sort the open list in an ascending order by F cost, remove its duplicates,
            ' and then choose its first item.
            openList = New List(Of AStarAlgorithm) _
                (From openPoint In openList Order By openPoint.costF Ascending Distinct)
            Dim currPoint As AStarAlgorithm = openList.First()
            ' Don't add the current point to the closed list repeatedly.
            If Not closedList.Contains(currPoint) Then closedList.Add(currPoint)
            openList.Remove(currPoint)

            If currPoint.gridIdx = finishPoint.gridIdx Then
                Dim resultPath As New List(Of GridIndex), route As New List(Of GridIndex)

                ' Traverse the path from the finish point back to the start.
                Dim finder As AStarAlgorithm = currPoint
                Do Until finder Is Nothing
                    resultPath.Add(finder.gridIdx)
                    finder = finder.PreviousRef
                Loop
                resultPath.Reverse()

                For i As Integer = 1 To resultPath.Count - 1 Step 1
                    Dim rawDirection As GridIndex = resultPath(i) - resultPath(i - 1)

                    ' Select the direction maximizing the dot product with the raw direction.
                    Dim adjustedDirection As GridIndex =
                        Aggregate direction As GridIndex In StandardDirections
                            Order By CalcDotProduct(rawDirection, direction) Descending
                                Into First()

                    route.Add(adjustedDirection)
                Next i

                Return route
            End If

            For Each direction As GridIndex In StandardDirections
                Dim nextGridIdx As GridIndex = currPoint.gridIdx + direction

                ' Note: `UBound(multiDimArray, dimension)` is used to get the upper
                ' bound of a specific dimension in VB.NET, which is equivalent to
                ' `multiDimArray.GetLength(dimension - 1) - 1` in C#, and might be
                ' more intuitive for VB.NET developers.

                If nextGridIdx.x < 0 OrElse nextGridIdx.x > UBound(walkableTerrain, 1) OrElse
                   nextGridIdx.y < 0 OrElse nextGridIdx.y > UBound(walkableTerrain, 2) Then
                    ' If out of bounds, skip to the next iteration of the loop.
                    Continue For
                ElseIf Not walkableTerrain(nextGridIdx.x, nextGridIdx.y) Then
                    ' If the terrain at the next grid index is not walkable, skip to
                    ' the next iteration.
                    Continue For
                End If

                Dim nextPoint As New AStarAlgorithm(nextGridIdx, currPoint)
                If closedList.Contains(nextPoint) Then Continue For

                With nextPoint
                    .costG = currPoint.costG + 1
                    .costH = SquaredMagnitude(.gridIdx, finishGridIdx)

                    .costF = .costG + .costH  ' Essential for the A* algorithm.
                End With

                ' If the next point is already in the open list and has a higher G cost,
                ' skip it, so that the same point within a worse path won't be processed
                ' multiple times.
                If Aggregate openPoint In openList
                       Into Any(nextPoint.gridIdx = openPoint.gridIdx AndAlso
                                nextPoint.costG > openPoint.costG) Then Continue For

                Dim isNextPointInOpenList As Boolean =
                    Aggregate openPoint As AStarAlgorithm In openList
                        Into Any(openPoint.gridIdx.Equals(nextPoint.gridIdx))

                If Not isNextPointInOpenList Then openList.Add(nextPoint)
            Next direction
        Loop

        Return New List(Of GridIndex)
    End Function
End Class