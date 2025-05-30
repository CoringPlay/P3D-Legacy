﻿Public Class MapScreen

    Inherits Screen

    Public Enum VisibleMode As Integer
        Always
        Temporary
        Unlock
        Register
    End Enum
    Public Const RasterSize As Integer = 32
    Public Shared mapOffsetX As Integer = 100
    Public Shared mapOffsetY As Integer = 75
    Const MapMoveSpeed As Integer = 3

    Dim flag() As Object = {}

    Dim regions As New List(Of String)
    Dim regionPointer As Integer = 0
    Dim currentRegion As String = ""

    Dim cities As New List(Of City)
    Dim routes As New List(Of Route)
    Dim places As New List(Of Place)
    Dim RoamingPoke As New List(Of Roaming)

    Dim PlayerSkinTexture As Texture2D
    Dim objectsTexture As Texture2D
    Dim mapTexture As Texture2D
    Dim texture As Texture2D

    Dim hoverText As String = ""
    Dim pokehoverText As String = ""
    Dim drawObjects(3) As Boolean
    Dim backgroundOffset As Single = 0.0F

    Dim CursorPosition As New Vector2(0)
    Dim lastMousePosition As Vector2

    Public Sub New(ByVal currentScreen As Screen, ByVal regions As List(Of String), ByVal startIndex As Integer, ByVal flag() As Object)
        Me.Identification = Identifications.MapScreen

        Me.PreScreen = currentScreen
        Me.flag = flag
        Me.currentRegion = regions(startIndex)
        Me.regions = regions
        Me.regionPointer = startIndex

        Me.drawObjects = Player.Temp.MapSwitch
        If Core.Player.IsGameJoltSave And GameJolt.API.LoggedIn Then
            Dim ownE As New GameJolt.Emblem(Core.Player.Name, Core.GameJoltSave.GameJoltID, Core.GameJoltSave.Points, Core.GameJoltSave.Gender, Core.GameJoltSave.Emblem)
            PlayerSkinTexture = ownE.SpriteTexture
        Else
            If Screen.Level.Surfing = True Then
                PlayerSkinTexture = TextureManager.GetTexture("Textures\NPC\" & Core.Player.TempSurfSkin)
            ElseIf Screen.Level.Riding = True Then
                PlayerSkinTexture = TextureManager.GetTexture("Textures\NPC\" & Core.Player.TempRideSkin)
            Else
                PlayerSkinTexture = TextureManager.GetTexture("Textures\NPC\" & Core.Player.Skin)
            End If
        End If
        Me.MouseVisible = False

        Me.objectsTexture = TextureManager.GetTexture("GUI\Map\map_objects")
        LoadMapTexture()

        Me.texture = TextureManager.GetTexture("GUI\Menus\General")

        Me.FillMap()

        Dim v As Vector2 = GetCursorPosition()
        If v.X <> 0 Or v.Y <> 0 Then
            Me.CursorPosition = Me.GetCursorPosition() + New Vector2(mapOffsetX, mapOffsetY)
        Else
            Me.CursorPosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If

        Me.lastMousePosition = Me.CursorPosition
        Mouse.SetPosition(CInt(CursorPosition.X), CInt(CursorPosition.Y))
    End Sub

    Public Sub New(ByVal currentScreen As Screen, ByVal startRegion As String, ByVal flag() As Object)
        Me.New(currentScreen, {startRegion}.ToList(), 0, flag)
    End Sub

    Private Sub LoadMapTexture()
        Me.mapTexture = TextureManager.GetTexture("GUI\Map\" & Me.currentRegion & "_map")
    End Sub

    Private Sub ResetScreen()
        mapOffsetX = 100
        mapOffsetY = 75

        For i = 0 To 3
            Me.drawObjects(i) = True
        Next
    End Sub

    Private Sub FillMap()
        mapOffsetX = 100
        mapOffsetY = 75

        Me.routes.Clear()
        Me.places.Clear()
        Me.cities.Clear()
        Me.RoamingPoke.Clear()

        Dim TempPoke As New List(Of Roaming)
        Dim RoamingPokeName As New List(Of String)

        Dim path As String = GameModeManager.GetScriptPath("worldmap\" & Me.currentRegion & ".dat")
        Security.FileValidation.CheckFileValid(path, False, "MapScreen.vb")

        Dim InputData() As String = System.IO.File.ReadAllLines(path)

        For Each line As String In InputData
            If line <> "" And line.StartsWith("{""") = True Then
                Dim Tags As New Dictionary(Of String, String)
                Dim Data() As String = line.Split(CChar("}"))
                For Each Tag As String In Data
                    If Tag.Contains("{") = True And Tag.Contains("[") = True Then
                        Dim TagName As String = Tag.Remove(0, 2)
                        TagName = TagName.Remove(TagName.IndexOf(""""))

                        Dim TagContent As String = Tag.Remove(0, Tag.IndexOf("[") + 1)
                        TagContent = TagContent.Remove(TagContent.IndexOf("]"))

                        Tags.Add(TagName.ToLower(), TagContent)
                    End If
                Next

                Select Case Tags("placetype").ToLower()
                    Case "city"
                        Dim Name As String = Tags("name")
                        Dim MapFiles() As String = Tags("mapfiles").Split(CChar(","))
                        Dim PositionList As List(Of String) = Tags("position").Split(CChar(",")).ToList()
                        Dim Size As String = Tags("size")

                        Dim CitySize As City.CitySize = City.CitySize.Small

                        Select Case Size.ToLower()
                            Case "small", "0"
                                CitySize = City.CitySize.Small
                            Case "vertical", "1"
                                CitySize = City.CitySize.Vertical
                            Case "horizontal", "2"
                                CitySize = City.CitySize.Horizontal
                            Case "big", "3"
                                CitySize = City.CitySize.Big
                            Case "large", "4"
                                CitySize = City.CitySize.Large
                        End Select
                        Dim PlayerPositionList As List(Of String) = "-1,-1".Split(CChar(",")).ToList()
                        If Tags.ContainsKey("playerposition") = True Then
                            PlayerPositionList = Tags("playerposition").Split(CChar(",")).ToList()
                        End If
                        Dim Visible As Integer = VisibleMode.Always
                        If Tags.ContainsKey("visible") = True Then
                            Select Case Tags("visible").ToLower()
                                Case "0", "always"
                                    Visible = VisibleMode.Always
                                Case "1", "temporary"
                                    Visible = VisibleMode.Temporary
                                Case "2", "unlock"
                                    Visible = VisibleMode.Unlock
                                Case "3", "register"
                                    Visible = VisibleMode.Register
                            End Select
                        End If
                        Dim Register As String = ""
                        If Tags.ContainsKey("register") = True Then
                            Register = Tags("register")
                        End If
                        If Tags.ContainsKey("flyto") = True Then
                            Dim FlyTo As New List(Of String)
                            FlyTo = Tags("flyto").Split(CChar(",")).ToList()
                            cities.Add(New City(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), CitySize, FlyTo(0), New Vector3(CSng(FlyTo(1)), CSng(FlyTo(2)), CSng(FlyTo(3))), CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        Else
                            cities.Add(New City(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), CitySize, "", Nothing, CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        End If
                    Case "route"
                        Dim Name As String = Tags("name")
                        Dim MapFiles() As String = Tags("mapfiles").Split(CChar(","))
                        Dim PositionList As List(Of String) = Tags("position").Split(CChar(",")).ToList()

                        Dim RouteDirection As Route.RouteDirections = Route.RouteDirections.Horizontal

                        Select Case Tags("direction").ToLower()
                            Case "horizontal", "0"
                                RouteDirection = Route.RouteDirections.Horizontal
                            Case "vertical", "1"
                                RouteDirection = Route.RouteDirections.Vertical
                            Case "horizontalendright", "2"
                                RouteDirection = Route.RouteDirections.HorizontalEndRight
                            Case "horizontalendleft", "3"
                                RouteDirection = Route.RouteDirections.HorizontalEndLeft
                            Case "verticalendup", "4"
                                RouteDirection = Route.RouteDirections.VerticalEndUp
                            Case "verticalenddown", "5"
                                RouteDirection = Route.RouteDirections.VerticalEndDown
                            Case "curvedownright", "6"
                                RouteDirection = Route.RouteDirections.CurveDownRight
                            Case "curvedownleft", "7"
                                RouteDirection = Route.RouteDirections.CurveDownLeft
                            Case "curveupleft", "8"
                                RouteDirection = Route.RouteDirections.CurveUpLeft
                            Case "curveupright", "9"
                                RouteDirection = Route.RouteDirections.CurveUpRight
                            Case "tup", "10"
                                RouteDirection = Route.RouteDirections.TUp
                            Case "tdown", "13"
                                RouteDirection = Route.RouteDirections.TDown
                            Case "tleft", "14"
                                RouteDirection = Route.RouteDirections.TLeft
                            Case "tright", "15"
                                RouteDirection = Route.RouteDirections.TRight
                            Case "horizontalconnection", "11"
                                RouteDirection = Route.RouteDirections.HorizontalConnection
                            Case "verticalconnection", "12"
                                RouteDirection = Route.RouteDirections.VerticalConnection
                        End Select

                        Dim RouteType As Route.RouteTypes = Route.RouteTypes.Land

                        Select Case Tags("routetype").ToLower()
                            Case "land", "0"
                                RouteType = Route.RouteTypes.Land
                            Case "water", "1"
                                RouteType = Route.RouteTypes.Water
                        End Select
                        Dim PlayerPositionList As List(Of String) = "-1,-1".Split(CChar(",")).ToList()
                        If Tags.ContainsKey("playerposition") = True Then
                            PlayerPositionList = Tags("playerposition").Split(CChar(",")).ToList()
                        End If

                        Dim Visible As Integer = VisibleMode.Always
                        If Tags.ContainsKey("visible") = True Then
                            Select Case Tags("visible").ToLower()
                                Case "0", "always"
                                    Visible = VisibleMode.Always
                                Case "1", "temporary"
                                    Visible = VisibleMode.Temporary
                                Case "2", "unlock"
                                    Visible = VisibleMode.Unlock
                                Case "3", "register"
                                    Visible = VisibleMode.Register
                            End Select
                        End If

                        Dim Register As String = ""
                        If Tags.ContainsKey("register") = True Then
                            Register = Tags("register")
                        End If
                        If Tags.ContainsKey("flyto") = True Then
                            Dim FlyTo As New List(Of String)
                            FlyTo = Tags("flyto").Split(CChar(",")).ToList()
                            routes.Add(New Route(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), RouteDirection, RouteType, FlyTo(0), New Vector3(CSng(FlyTo(1)), CSng(FlyTo(2)), CSng(FlyTo(3))), CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        Else
                            routes.Add(New Route(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), RouteDirection, RouteType, "", Nothing, CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        End If
                    Case "place"
                        Dim Name As String = Tags("name")
                        Dim MapFiles() As String = Tags("mapfiles").Split(CChar(","))
                        Dim PositionList As List(Of String) = Tags("position").Split(CChar(",")).ToList()
                        Dim Size As String = Tags("size")

                        Dim PlaceSize As Place.PlaceSizes = Place.PlaceSizes.Small

                        Select Case Size.ToLower()
                            Case "small", "0"
                                PlaceSize = Place.PlaceSizes.Small
                            Case "vertical", "1"
                                PlaceSize = Place.PlaceSizes.Vertical
                            Case "round", "2"
                                PlaceSize = Place.PlaceSizes.Round
                            Case "square", "3"
                                PlaceSize = Place.PlaceSizes.Square
                            Case "verticalbig", "4"
                                PlaceSize = Place.PlaceSizes.VerticalBig
                            Case "large", "5"
                                PlaceSize = Place.PlaceSizes.Large
                        End Select
                        Dim PlayerPositionList As List(Of String) = "-1,-1".Split(CChar(",")).ToList()
                        If Tags.ContainsKey("playerposition") = True Then
                            PlayerPositionList = Tags("playerposition").Split(CChar(",")).ToList()
                        End If

                        Dim Visible As Integer = VisibleMode.Always
                        If Tags.ContainsKey("visible") = True Then
                            Select Case Tags("visible").ToLower()
                                Case "0", "always"
                                    Visible = VisibleMode.Always
                                Case "1", "temporary"
                                    Visible = VisibleMode.Temporary
                                Case "2", "unlock"
                                    Visible = VisibleMode.Unlock
                                Case "3", "register"
                                    Visible = VisibleMode.Register
                            End Select
                        End If

                        Dim Register As String = ""
                        If Tags.ContainsKey("register") = True Then
                            Register = Tags("register")
                        End If

                        If Tags.ContainsKey("flyto") = True Then
                            Dim FlyTo As New List(Of String)
                            FlyTo = Tags("flyto").Split(CChar(",")).ToList()
                            places.Add(New Place(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), PlaceSize, FlyTo(0), New Vector3(CSng(FlyTo(1)), CSng(FlyTo(2)), CSng(FlyTo(3))), CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        Else
                            places.Add(New Place(Name, MapFiles, CInt(PositionList(0)), CInt(PositionList(1)), PlaceSize, "", Nothing, CInt(PlayerPositionList(0)), CInt(PlayerPositionList(1)), Visible, Register))
                        End If
                End Select

                If Not String.IsNullOrWhiteSpace(Core.Player.RoamingPokemonData) Then
                    If Core.Player.RoamingPokemonData.Length > 0 AndAlso Core.Player.RoamingPokemonData.Contains("|") Then
                        For Each Pokes As String In Core.Player.RoamingPokemonData.SplitAtNewline
                            ' RoamerID,PokémonID,Level,regionID,startLevelFile,MusicLoop,[Shiny],[ScriptPath]
                            Dim TempData() As String = Pokes.Split("|")
                            Dim MapFiles() As String = Tags("mapfiles").Split(",")
                            Dim PokeCurrentLocation As String = TempData(4)
                            If MapFiles.Contains(PokeCurrentLocation) Then
                                TempPoke.Add(New Roaming(TempData(1), CInt(Tags("position").Split(",")(0)), CInt(Tags("position").Split(",")(1)), Tags("name")))
                            End If

                            Dim PokemonID As String = TempData(1)
                            Dim PokemonAddition As String = "xXx"
                            If PokemonID.Contains("_") Then
                                PokemonAddition = PokemonForms.GetAdditionalValueFromDataFile(TempData(1))
                                PokemonID = TempData(1).GetSplit(0, "_")
                            End If
                            If PokemonID.Contains(";") Then
                                PokemonAddition = TempData(1).GetSplit(1, ";")
                                PokemonID = TempData(1).GetSplit(0, ";")
                            End If
                            If RoamingPokeName Is Nothing OrElse Not RoamingPokeName.Contains(Pokemon.GetPokemonByID(CInt(PokemonID), PokemonAddition).GetName) Then
                                RoamingPokeName.Add(Pokemon.GetPokemonByID(CInt(PokemonID), PokemonAddition).GetName)
                            End If
                        Next
                    End If
                End If
            End If
        Next

        If TempPoke.Count > 0 And RoamingPokeName.Count > 0 Then
            For Each Pokes As String In RoamingPokeName
                Dim MapObject As List(Of Roaming) = (From p As Roaming In TempPoke Where p.Name = Pokes Order By p.Distance Ascending).ToList()
                If MapObject IsNot Nothing AndAlso Not MapObject.Count = 0 Then
                    RoamingPoke.Add(MapObject.ElementAt(MapObject(0).getSkipIndex))
                End If
            Next
        End If
    End Sub

    Public Overrides Sub Update()
        If lastMousePosition <> New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y) Then
            Me.CursorPosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
            Me.lastMousePosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If

        If Controls.Dismiss() = True Then
            SoundManager.PlaySound("select")
            Player.Temp.MapSwitch = Me.drawObjects
            Core.SetScreen(New TransitionScreen(Me, Me.PreScreen, Color.Black, False))
        End If

        If Controls.Up(False, True, False, True, True, True) = True Then
            CursorPosition.Y -= MapMoveSpeed * 2.0F
            Mouse.SetPosition(CInt(CursorPosition.X), CInt(CursorPosition.Y))
            Me.lastMousePosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If
        If Controls.Down(False, True, False, True, True, True) = True Then
            CursorPosition.Y += MapMoveSpeed * 2.0F
            Mouse.SetPosition(CInt(CursorPosition.X), CInt(CursorPosition.Y))
            Me.lastMousePosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If
        If Controls.Left(False, True, False, True, True, True) = True Then
            CursorPosition.X -= MapMoveSpeed * 2.0F
            Mouse.SetPosition(CInt(CursorPosition.X), CInt(CursorPosition.Y))
            Me.lastMousePosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If
        If Controls.Right(False, True, False, True, True, True) = True Then
            CursorPosition.X += MapMoveSpeed * 2.0F
            Mouse.SetPosition(CInt(CursorPosition.X), CInt(CursorPosition.Y))
            Me.lastMousePosition = New Vector2(MouseHandler.MousePosition.X, MouseHandler.MousePosition.Y)
        End If

        'If New Rectangle(0, 0, 50, Core.windowSize.Height).Contains(New Point(CInt(CursorPosition.X), CInt(CursorPosition.Y))) = True Then
        '    mapOffsetX += MapMoveSpeed
        'End If
        'If New Rectangle(0, 0, Core.windowSize.Width, 50).Contains(New Point(CInt(CursorPosition.X), CInt(CursorPosition.Y))) = True Then
        '    mapOffsetY += MapMoveSpeed
        'End If
        'If New Rectangle(Core.windowSize.Width - 50, 0, 50, Core.windowSize.Height).Contains(New Point(CInt(CursorPosition.X), CInt(CursorPosition.Y))) = True Then
        '    mapOffsetX -= MapMoveSpeed
        'End If
        'If New Rectangle(0, Core.windowSize.Height - 50, Core.windowSize.Width, 50).Contains(New Point(CInt(CursorPosition.X), CInt(CursorPosition.Y))) = True Then
        '    mapOffsetY -= MapMoveSpeed
        'End If

        Dim mapOffset As New Vector2(MapScreen.mapOffsetX, MapScreen.mapOffsetY)
        Dim cursorPoint As New Point(CInt(CursorPosition.X), CInt(CursorPosition.Y))

        Me.hoverText = ""
        Me.pokehoverText = ""

        If hoverText = "" And pokehoverText = "" And drawObjects(3) = True Then
            For Each Poke As Roaming In RoamingPoke
                If Poke.getRectangle(mapOffset).Contains(cursorPoint) = True Then
                    pokehoverText = Poke.Name
                    hoverText = Poke.Location
                    Exit For
                End If
            Next
        End If
        If hoverText = "" And pokehoverText = "" And drawObjects(2) = True Then
            For Each Place As Place In places
                If Place.getRectangle(mapOffset).Contains(cursorPoint) = True Then
                    Dim DoUpdate As Boolean = False
                    If Place.Visible = VisibleMode.Always OrElse Place.Visible = VisibleMode.Temporary AndAlso Place.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                        DoUpdate = True
                    ElseIf Place.Visible = VisibleMode.Unlock Then
                        For Each p As String In Place.ContainFiles
                            If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                                DoUpdate = True
                            End If
                            Exit For
                        Next
                    ElseIf Place.Visible = VisibleMode.Register Then
                        If ActionScript.IsRegistered(Place.Register) = True Then
                            DoUpdate = True
                        End If
                    End If
                    If DoUpdate = True Then
                        If Controls.Accept(True, True, True) = True Then
                            Place.Click(flag)
                        End If
                        hoverText = Place.Name
                    End If
                    Exit For
                End If
            Next
        End If
        If hoverText = "" And pokehoverText = "" And drawObjects(0) = True Then
            For Each City As City In cities
                If City.getRectangle(mapOffset).Contains(cursorPoint) = True Then
                    Dim DoUpdate As Boolean = False
                    If City.Visible = VisibleMode.Always OrElse City.Visible = VisibleMode.Temporary AndAlso City.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                        DoUpdate = True
                    ElseIf City.Visible = VisibleMode.Unlock Then
                        For Each p As String In City.ContainFiles
                            If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                                DoUpdate = True
                            End If
                            Exit For
                        Next
                    ElseIf City.Visible = VisibleMode.Register Then
                        If ActionScript.IsRegistered(City.Register) = True Then
                            DoUpdate = True
                        End If
                    End If
                    If DoUpdate = True Then
                        If Controls.Accept(True, True, True) = True Then
                            City.Click(flag)
                        End If
                        hoverText = City.Name
                    End If
                    Exit For
                End If
            Next
        End If
        If hoverText = "" And pokehoverText = "" And drawObjects(1) = True Then
            For Each Route As Route In routes
                If Route.getRectangle(mapOffset).Contains(cursorPoint) = True Then
                    Dim DoUpdate As Boolean = False
                    If Route.Visible = VisibleMode.Always OrElse Route.Visible = VisibleMode.Temporary AndAlso Route.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                        DoUpdate = True
                    ElseIf Route.Visible = VisibleMode.Unlock Then
                        For Each p As String In Route.ContainFiles
                            If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                                DoUpdate = True
                            End If
                            Exit For
                        Next
                    ElseIf Route.Visible = VisibleMode.Register Then
                        If ActionScript.IsRegistered(Route.Register) = True Then
                            DoUpdate = True
                        End If
                    End If
                    If DoUpdate = True Then
                        If Controls.Accept(True, True, True) = True Then
                            Route.Click(flag)
                        End If
                        hoverText = Route.Name
                    End If
                    Exit For
                End If
            Next
        End If

        backgroundOffset += 1.0F
        If backgroundOffset >= 64.0F Then
            backgroundOffset = 0.0F
        End If

        UpdateSwitch()

        Dim cPointer As Integer = Me.regionPointer
        If KeyBoardHandler.KeyPressed(Keys.LeftShift) = True Or ControllerHandler.ButtonPressed(Buttons.LeftTrigger) = True Then
            regionPointer -= 1
        End If
        If KeyBoardHandler.KeyPressed(Keys.RightShift) = True Or ControllerHandler.ButtonPressed(Buttons.RightTrigger) = True Then
            regionPointer += 1
        End If

        If regionPointer < 0 Then
            regionPointer = Me.regions.Count - 1
        ElseIf regionPointer > Me.regions.Count - 1 Then
            regionPointer = 0
        End If

        ' regionPointer = regionPointer.Clamp(0, Me.regions.Count - 1)

        If regionPointer <> cPointer Then
            Me.currentRegion = regions(regionPointer)

            LoadMapTexture()

            Me.FillMap()
        End If
    End Sub

    Private Sub UpdateSwitch()
        For i = 0 To 3
            Dim r As New Rectangle(Core.windowSize.Width - 240, 100 + i * 30, 240, 30)
            If Controls.Accept(True, True, True) = True Then
                If r.Contains(New Point(CInt(MouseHandler.MousePosition.X), CInt(MouseHandler.MousePosition.Y))) = True Then
                    Me.drawObjects(i) = Not Me.drawObjects(i)
                End If
            End If
        Next
    End Sub

    Public Overrides Sub Draw()
        Canvas.DrawRectangle(Core.windowSize, New Color(84, 198, 216))

        For y = 0 To Core.windowSize.Height Step 64
            Core.SpriteBatch.Draw(Me.texture, New Rectangle(Core.windowSize.Width - 128, y, 128, 64), New Rectangle(48, 0, 16, 16), Color.White)
        Next

        Core.SpriteBatch.Draw(Me.objectsTexture, New Rectangle(mapOffsetX + 15, mapOffsetY + 15, CInt(mapTexture.Width / 16) * MapScreen.RasterSize * 2, CInt(mapTexture.Height / 16) * MapScreen.RasterSize * 2), New Rectangle(96, 40, 16, 16), New Color(0, 0, 0, 100))

        Dim mapOffset As New Vector2(MapScreen.mapOffsetX, MapScreen.mapOffsetY)

        For x = 0 To mapTexture.Width / 16
            If x * 16 <= mapTexture.Width - 16 Then
                For y = 0 To mapTexture.Height / 16
                    If y * 16 <= mapTexture.Height - 16 Then
                        Core.SpriteBatch.Draw(Me.mapTexture, New Rectangle(CInt(x * MapScreen.RasterSize * 2 + mapOffset.X), CInt(y * MapScreen.RasterSize * 2 + mapOffset.Y), MapScreen.RasterSize * 2, MapScreen.RasterSize * 2), New Rectangle(CInt(x * 16), CInt(y * 16), 16, 16), Color.White)
                    End If
                Next
            End If
        Next

        If drawObjects(1) = True Then
            For Each Route As Route In routes
                Dim isSelected As Boolean = False
                If Route.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    isSelected = True
                End If

                Dim c As Color = Color.White
                If flag(0).ToString().ToLower() = "fly" And Route.CanFlyTo(flag) = False Then
                    c = Color.Gray
                End If

                Dim DoDraw As Boolean = False
                If Route.Visible = VisibleMode.Always OrElse Route.Visible = VisibleMode.Temporary AndAlso Route.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    DoDraw = True
                ElseIf Route.Visible = VisibleMode.Unlock Then
                    For Each p As String In Route.ContainFiles
                        If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                            DoDraw = True
                        End If
                        Exit For
                    Next
                ElseIf Route.Visible = VisibleMode.Register Then
                    If ActionScript.IsRegistered(Route.Register) = True Then
                        DoDraw = True
                    End If
                End If
                If DoDraw = True Then
                    Core.SpriteBatch.Draw(Route.getTexture(objectsTexture, isSelected), Route.getRectangle(mapOffset), c)
                End If
            Next
        End If

        If drawObjects(0) = True Then
            For Each City As City In Me.cities
                Dim isSelected As Boolean = False
                If City.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    isSelected = True
                End If

                Dim c As Color = Color.White
                If flag(0).ToString().ToLower() = "fly" And City.CanFlyTo(flag) = False Then
                    c = Color.Gray
                End If

                Dim DoDraw As Boolean = False
                If City.Visible = VisibleMode.Always OrElse City.Visible = VisibleMode.Temporary AndAlso City.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    DoDraw = True
                ElseIf City.Visible = VisibleMode.Unlock Then
                    For Each p As String In City.ContainFiles
                        If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                            DoDraw = True
                        End If
                        Exit For
                    Next
                ElseIf City.Visible = VisibleMode.Register Then
                    If ActionScript.IsRegistered(City.Register) = True Then
                        DoDraw = True
                    End If
                End If
                If DoDraw = True Then
                    Core.SpriteBatch.Draw(City.getTexture(objectsTexture, isSelected), City.getRectangle(mapOffset), c)
                End If
            Next
        End If

        If drawObjects(2) = True Then
            For Each Place As Place In places
                Dim isSelected As Boolean = False
                If Place.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    isSelected = True
                End If

                Dim c As Color = Color.White
                If flag(0).ToString().ToLower() = "fly" And Place.CanFlyTo(flag) = False Then
                    c = Color.Gray
                End If

                Dim DoDraw As Boolean = False
                If Place.Visible = VisibleMode.Always OrElse Place.Visible = VisibleMode.Temporary AndAlso Place.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                    DoDraw = True
                ElseIf Place.Visible = VisibleMode.Unlock Then
                    For Each p As String In Place.ContainFiles
                        If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(p.ToLower()) = True Then
                            DoDraw = True
                        End If
                        Exit For
                    Next
                ElseIf Place.Visible = VisibleMode.Register Then
                    If ActionScript.IsRegistered(Place.Register) = True Then
                        DoDraw = True
                    End If
                End If
                If DoDraw = True Then
                    Core.SpriteBatch.Draw(Place.getTexture(objectsTexture, isSelected), Place.getRectangle(mapOffset), c)
                End If
            Next
        End If

        If drawObjects(3) = True Then
            For Each Pokes As Roaming In RoamingPoke
                Core.SpriteBatch.Draw(Pokes.getTexture(), Pokes.getRectangle(mapOffset), Color.White)
            Next
        End If

        Dim PlayerSkinWidth As Integer = CInt(PlayerSkinTexture.Width / 3)
        Dim PlayerSkinHeight As Integer = CInt(PlayerSkinTexture.Height / 4)
        Dim PlayerSkinScale As Single = 1.0F
        If PlayerSkinTexture.Width = PlayerSkinTexture.Height / 2 Then
            PlayerSkinWidth = CInt(PlayerSkinTexture.Width / 2)
        ElseIf PlayerSkinTexture.Width = PlayerSkinTexture.Height Then
            PlayerSkinWidth = CInt(PlayerSkinTexture.Width / 4)
        End If
        If PlayerSkinWidth > 32 Then
            PlayerSkinScale = 0.5F
        End If

        Dim PlayerTextureRectangle As Rectangle = New Rectangle(0, CInt(PlayerSkinHeight * 2), PlayerSkinWidth, PlayerSkinHeight)

        Dim v As Vector2 = GetPlayerPosition()
        If v.X <> 0 Or v.Y <> 0 Then
            Core.SpriteBatch.Draw(PlayerSkinTexture, New Rectangle(CInt(GetPlayerPosition.X + mapOffsetX - PlayerSkinWidth * PlayerSkinScale), CInt(GetPlayerPosition.Y + mapOffsetY - PlayerSkinHeight * PlayerSkinScale), CInt(PlayerSkinWidth * 2 * PlayerSkinScale), CInt(PlayerSkinHeight * 2 * PlayerSkinScale)), PlayerTextureRectangle, Color.White)
        End If

        If Me.hoverText <> "" And Me.pokehoverText <> "" Then
            Dim hoverString As String = Localization.GetString("map_screen_PokemonAtPlace", "[NAME] at [PLACE]").Replace("[NAME]", Localization.GetString("pokemon_name_" & Me.pokehoverText)).Replace("[PLACE]", Localization.GetString("Places_" & Me.hoverText))
            Core.SpriteBatch.DrawRectangle(New Rectangle(CInt(Me.CursorPosition.X + 32 - 2), CInt(Me.CursorPosition.Y - 32 + 2), CInt(FontManager.MainFont.MeasureString(hoverString).X + 2 + 4), CInt(FontManager.MainFont.MeasureString(hoverString).Y)), New Color(0, 0, 0, 0.6F))
            Core.SpriteBatch.DrawString(FontManager.MainFont, hoverString, New Vector2(CInt(Me.CursorPosition.X + 32 + 2), CInt(Me.CursorPosition.Y - 32 + 2)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, hoverString, New Vector2(CInt(Me.CursorPosition.X + 32), CInt(Me.CursorPosition.Y - 32)), Color.White)
        ElseIf Me.hoverText <> "" And Me.pokehoverText = "" Then
            Dim hoverString As String = Localization.GetString("Places_" & Me.hoverText)
            Core.SpriteBatch.DrawRectangle(New Rectangle(CInt(Me.CursorPosition.X + 32 - 2), CInt(Me.CursorPosition.Y - 32 + 2), CInt(FontManager.MainFont.MeasureString(hoverString).X + 2 + 4), CInt(FontManager.MainFont.MeasureString(hoverString).Y)), New Color(0, 0, 0, 0.6F))
            Core.SpriteBatch.DrawString(FontManager.MainFont, hoverString, New Vector2(Me.CursorPosition.X + 32 + 2, Me.CursorPosition.Y - 32 + 2), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, hoverString, New Vector2(Me.CursorPosition.X + 32, Me.CursorPosition.Y - 32), Color.White)
        End If

        Dim regionString As String = Localization.GetString(Me.currentRegion(0).ToString().ToUpper() & Me.currentRegion.Remove(0, 1))
        If Me.regions.Count > 1 Then
            regionString &= " " & Localization.GetString("map_screen_RegionSwitchHint", "(Press the Shift Key/Shoulder Triggers to switch between regions.)")
        End If

        Core.SpriteBatch.DrawString(FontManager.InGameFont, regionString, New Vector2(MapScreen.mapOffsetX + 2, MapScreen.mapOffsetY - 31), Color.Black)
        Core.SpriteBatch.DrawString(FontManager.InGameFont, regionString, New Vector2(MapScreen.mapOffsetX, MapScreen.mapOffsetY - 33), Color.White)

        DrawSwitch()
        DrawCursor()
    End Sub

    Private Sub DrawSwitch()
        ' Cities:
        Dim r As New Rectangle(104, 0, 12, 12)
        If drawObjects(0) = False Then
            r = New Rectangle(116, 0, 12, 12)
        End If
        Core.SpriteBatch.Draw(Me.objectsTexture, New Rectangle(Core.windowSize.Width - 256 - 48, 100, 24, 24), r, New Color(255, 255, 255, 220))
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_cities"), New Vector2(Core.windowSize.Width - 256 + 2, 100 + 2), Color.Black)
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_cities"), New Vector2(Core.windowSize.Width - 256, 100), Color.White)
        ' Routes:
        r = New Rectangle(104, 12, 12, 12)
        If drawObjects(1) = False Then
            r = New Rectangle(116, 12, 12, 12)
        End If
        Core.SpriteBatch.Draw(Me.objectsTexture, New Rectangle(Core.windowSize.Width - 256 - 48, 130, 24, 24), r, New Color(255, 255, 255, 220))
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_routes"), New Vector2(Core.windowSize.Width - 256 + 2, 130 + 2), Color.Black)
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_routes"), New Vector2(Core.windowSize.Width - 256, 130), Color.White)
        ' Places:
        r = New Rectangle(104, 24, 12, 12)
        If drawObjects(2) = False Then
            r = New Rectangle(116, 24, 12, 12)
        End If
        Core.SpriteBatch.Draw(Me.objectsTexture, New Rectangle(Core.windowSize.Width - 256 - 48, 160, 24, 24), r, New Color(255, 255, 255, 220))
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_places"), New Vector2(Core.windowSize.Width - 256 + 2, 160 + 2), Color.Black)
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_places"), New Vector2(Core.windowSize.Width - 256, 160), Color.White)
        ' Roaming:
        r = New Rectangle(113, 65, 14, 14)
        If drawObjects(3) = False Then
            r = New Rectangle(113, 81, 14, 14)
        End If
        Core.SpriteBatch.Draw(Me.objectsTexture, New Rectangle(Core.windowSize.Width - 256 - 48, 187, 28, 28), r, New Color(255, 255, 255, 220))
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_roaming"), New Vector2(Core.windowSize.Width - 256 + 2, 190 + 2), Color.Black)
        Core.SpriteBatch.DrawString(FontManager.MainFont, Localization.GetString("map_screen_roaming"), New Vector2(Core.windowSize.Width - 256, 190), Color.White)
    End Sub

    Private Sub DrawCursor()
        Dim t As Texture2D = TextureManager.GetTexture("GUI\Menus\General", New Rectangle(0, 0, 16, 16), "")
        Core.SpriteBatch.Draw(t, New Rectangle(CInt(CursorPosition.X), CInt(CursorPosition.Y) - 30, 32, 32), Color.White)
    End Sub

    Public Shared Sub UseFly(ByVal FlyToFile As String, ByVal FlyToPosition As Vector3, ByVal flag() As Object)
        Screen.Camera.PlannedMovement = New Vector3(0, 2, 0)

        Dim p As Pokemon = CType(flag(1), Pokemon)
        Dim skinName As String = Screen.Level.OwnPlayer.SkinName

        If Screen.Level.Surfing = True Then
            skinName = Core.Player.TempSurfSkin
            Screen.Level.Surfing = False
            Screen.Level.OverworldPokemon.Visible = False
        End If
        If Screen.Level.Riding = True Then
            skinName = Core.Player.TempRideSkin
            Screen.Level.Riding = False
        End If

        Dim isShiny As String = "N"
        If Not p Is Nothing Then
            If p.IsShiny = True Then
                isShiny = "S"
            End If
        End If

        Dim s As String = "version=2" & Environment.NewLine &
            "@text.show(" & p.GetDisplayName() & " " & Localization.GetString("fieldmove_fly_used", "used~Fly!") & ")" & Environment.NewLine

        If CType(Screen.Camera, OverworldCamera).ThirdPerson = False Then
            s &= "@camera.activateThirdPerson" & Environment.NewLine
        End If

        s &= "@camera.setposition(0,0.9,3)" & Environment.NewLine &
            "@level.wait(30)" & Environment.NewLine &
            "@pokemon.cry(" & p.Number & ")" & Environment.NewLine &
            "@player.wearskin([POKEMON|" & isShiny & "]" & p.Number & PokemonForms.GetOverworldAddition(p) & ")" & Environment.NewLine &
            "@player.turnto(2)" & Environment.NewLine &
            "@player.move(2)" & Environment.NewLine &
            "@sound.play(FieldMove_Fly)" & Environment.NewLine &
            "@camera.fix" & Environment.NewLine &
            "@player.setmovement(0,2,3)" & Environment.NewLine &
            "@player.move(3)" & Environment.NewLine &
            "@screen.fadeout(10)" & Environment.NewLine &
            "@camera.defix" & Environment.NewLine &
            "@camera.reset" & Environment.NewLine &
            "@player.turnto(0)" & Environment.NewLine &
            "@player.warp(" & FlyToFile & "," & FlyToPosition.X.ToString().ReplaceDecSeparator() & "," & (FlyToPosition.Y - 4 + 0.1F).ToString().ReplaceDecSeparator() & "," & (FlyToPosition.Z + 6).ToString().ReplaceDecSeparator() & ",0)" & Environment.NewLine &
            "@camera.setyaw(0)" & Environment.NewLine &
            "@camera.setposition(0,-3.7,-4.5)" & Environment.NewLine &
            "@level.update" & Environment.NewLine &
            "@player.setmovement(0,-2,-3)" & Environment.NewLine &
            "@screen.fadein(10)" & Environment.NewLine &
            "@camera.fix" & Environment.NewLine &
            "@player.move(2)" & Environment.NewLine &
            "@camera.reset" & Environment.NewLine &
            "@camera.defix(1)" & Environment.NewLine &
            "@player.setmovement(0,-2,0)" & Environment.NewLine &
            "@player.move(2)" & Environment.NewLine &
            "@player.turnto(2)" & Environment.NewLine &
            "@player.wearskin(" & skinName & ")" & Environment.NewLine

        While Core.CurrentScreen.Identification <> Identifications.OverworldScreen
            If Core.CurrentScreen.PreScreen.Identification = Identifications.OverworldScreen Then
                Core.SetScreen(New TransitionScreen(Core.CurrentScreen, Core.CurrentScreen.PreScreen, Color.White, False))
                Exit While
            Else
                Core.SetScreen(Core.CurrentScreen.PreScreen)
            End If
        End While

        If CType(Screen.Camera, OverworldCamera).ThirdPerson = False Then
            s &= "@camera.deactivatethirdperson" & Environment.NewLine
        End If

        s &= "@level.wait(1)" & Environment.NewLine &
             ":end"
        PlayerStatistics.Track("Fly used", 1)
        Core.Player.IsFlying = True
        CType(CType(Core.CurrentScreen, TransitionScreen).NewScreen, OverworldScreen).ActionScript.StartScript(s, 2, False)
    End Sub

    Private Function GetCursorPosition() As Vector2
        Dim v As Vector2 = New Vector2(0, 0)
        Dim r As New Rectangle(0, 0, 0, 0)
        Dim mapOffset As New Vector2(mapOffsetX, mapOffsetY)

        For Each City As City In Me.cities
            If City.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                v = City.getPosition()
                r = City.getRectangle(mapOffset)
            End If
        Next
        For Each Place As Place In Me.places
            If Place.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                v = Place.getPosition()
                r = Place.getRectangle(mapOffset)
            End If
        Next
        For Each Route As Route In Me.routes
            If Route.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                v = Route.getPosition()
                r = Route.getRectangle(mapOffset)
            End If
        Next

        Return (v + New Vector2(CInt(r.Width / 2), CInt(r.Height / 2)))
    End Function
    Private Function GetPlayerPosition() As Vector2
        Dim v As Vector2 = New Vector2(0, 0)
        Dim r As New Rectangle(0, 0, 0, 0)
        Dim mapOffset As New Vector2(mapOffsetX, mapOffsetY)
        Dim sizeX = 1 * MapScreen.RasterSize
        Dim sizeY = 1 * MapScreen.RasterSize

        For Each City As City In Me.cities
            If City.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                If City.PlayerPositionX <> -1 AndAlso City.PlayerPositionY <> -1 Then
                    v = City.getPlayerPosition()
                    r = New Rectangle(CInt(City.getPlayerPosition().X + mapOffset.X), CInt(City.getPosition().Y + mapOffset.Y), sizeX, sizeY)
                Else
                    v = City.getPosition()
                    r = City.getRectangle(mapOffset)
                End If
            End If
        Next
        For Each Place As Place In Me.places
            If Place.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                If Place.PlayerPositionX <> -1 AndAlso Place.PlayerPositionY <> -1 Then
                    v = Place.getPlayerPosition()
                    r = New Rectangle(CInt(Place.getPlayerPosition().X + mapOffset.X), CInt(Place.getPosition().Y + mapOffset.Y), sizeX, sizeY)
                Else
                    v = Place.getPosition()
                    r = Place.getRectangle(mapOffset)
                End If
            End If
        Next
        For Each Route As Route In Me.routes
            If Route.ContainFiles.Contains(Level.LevelFile.ToLower()) = True Then
                If Route.PlayerPositionX <> -1 AndAlso Route.PlayerPositionY <> -1 Then
                    v = Route.getPlayerPosition()
                    r = New Rectangle(CInt(Route.getPlayerPosition().X + mapOffset.X), CInt(Route.getPosition().Y + mapOffset.Y), sizeX, sizeY)
                Else
                    v = Route.getPosition()
                    r = Route.getRectangle(mapOffset)
                End If
            End If
        Next

        Return (v + New Vector2(CInt(r.Width / 2), CInt(r.Height / 2)))
    End Function

    Public Class City

        Public Enum CitySize
            Small
            Vertical
            Horizontal
            Big
            Large
        End Enum

        Public Name As String = "???"
        Public ContainFiles As New List(Of String)
        Public PlayerPositionX As Integer = -1
        Public PlayerPositionY As Integer = -1
        Public PositionX As Integer = 0
        Public PositionY As Integer = 0
        Public FlyToFile As String = ""
        Public FlyToPosition As Vector3 = New Vector3(0)
        Public Size As CitySize = CitySize.Small
        Public Visible As Integer = VisibleMode.Always
        Public Register As String = ""

        Dim T As Texture2D = Nothing

        Public Sub New(ByVal Name As String, ByVal ContainFiles() As String, ByVal PositionX As Integer, ByVal PositionY As Integer, ByVal Size As CitySize, Optional ByVal FlyToFile As String = "", Optional ByVal FlyToPosition As Vector3 = Nothing, Optional PlayerPositionX As Integer = -1, Optional PlayerPositionY As Integer = -1, Optional Visible As Integer = VisibleMode.Always, Optional Register As String = "")
            Me.Name = Name

            For Each file As String In ContainFiles
                Me.ContainFiles.Add(file.ToLower())
            Next

            Me.PositionX = PositionX
            Me.PositionY = PositionY
            If PlayerPositionX <> -1 Then
                Me.PlayerPositionX = PlayerPositionX
            Else
                Me.PlayerPositionX = PositionX
            End If
            If PlayerPositionY <> -1 Then
                Me.PlayerPositionY = PlayerPositionY
            Else
                Me.PlayerPositionY = PositionY
            End If
            Me.Size = Size

            Me.FlyToFile = FlyToFile
            Me.FlyToPosition = FlyToPosition
            Me.Visible = Visible
            Me.Register = Register
        End Sub

        Public Function getPosition() As Vector2
            Return New Vector2(Me.PositionX * MapScreen.RasterSize, Me.PositionY * MapScreen.RasterSize)
        End Function

        Public Function getPlayerPosition() As Vector2
            Return New Vector2(Me.PlayerPositionX * MapScreen.RasterSize, Me.PlayerPositionY * MapScreen.RasterSize)
        End Function

        Public Function getRectangle(ByVal offset As Vector2) As Rectangle
            Dim sizeX As Integer = 0
            Dim sizeY As Integer = 0

            Select Case Me.Size
                Case CitySize.Small
                    sizeX = 1
                    sizeY = 1
                Case CitySize.Horizontal
                    sizeX = 2
                    sizeY = 1
                Case CitySize.Vertical
                    sizeX = 1
                    sizeY = 2
                Case CitySize.Big
                    sizeX = 2
                    sizeY = 2
                Case CitySize.Large
                    sizeX = 3
                    sizeY = 2
            End Select

            sizeX *= MapScreen.RasterSize
            sizeY *= MapScreen.RasterSize

            Return New Rectangle(CInt(Me.getPosition().X + offset.X), CInt(Me.getPosition().Y + offset.Y), sizeX, sizeY)
        End Function

        Public Function getTexture(ByVal FullTexture As Texture2D, ByVal isSelected As Boolean) As Texture2D
            If Me.T Is Nothing Or isSelected = True Then
                Dim r As Rectangle

                Dim modX As Integer = 0
                If isSelected = True Then
                    modX = 36
                End If

                Select Case Me.Size
                    Case CitySize.Small
                        r = New Rectangle(0 + modX, 0, 12, 12)
                    Case CitySize.Vertical
                        r = New Rectangle(0 + modX, 12, 12, 24)
                    Case CitySize.Horizontal
                        r = New Rectangle(12 + modX, 0, 24, 12)
                    Case CitySize.Big
                        r = New Rectangle(12 + modX, 12, 24, 24)
                    Case CitySize.Large
                        r = New Rectangle(0 + modX, 36, 36, 24)
                End Select
                Me.T = TextureManager.GetTexture(FullTexture, r)
            End If

            Return Me.T
        End Function

        Public Sub Click(ByVal flag() As Object)
            Select Case flag(0).ToString().ToLower()
                Case "fly"
                    If CanFlyTo(flag) Then
                        MapScreen.UseFly(FlyToFile, FlyToPosition, flag)
                    End If
            End Select
        End Sub

        Public Function CanFlyTo(ByVal flag() As Object) As Boolean
            If flag(0).ToString().ToLower() = "fly" Then
                If FlyToPosition <> Nothing And FlyToFile <> "" Then
                    If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(FlyToFile.ToLower()) = True Or GameController.IS_DEBUG_ACTIVE = True Or Core.Player.SandBoxMode = True Then
                        Return True
                    End If
                End If
            End If
            Return False
        End Function

    End Class

    Public Class Route

        Public Enum RouteTypes
            Land
            Water
        End Enum

        Public Enum RouteDirections
            Horizontal
            Vertical

            HorizontalEndRight
            HorizontalEndLeft

            VerticalEndUp
            VerticalEndDown

            CurveDownRight
            CurveDownLeft
            CurveUpLeft
            CurveUpRight

            TUp
            TDown
            TRight
            TLeft

            HorizontalConnection
            VerticalConnection
        End Enum

        Public Name As String = ""
        Public PositionX As Integer = 0
        Public PositionY As Integer = 0
        Public PlayerPositionX As Integer = -1
        Public PlayerPositionY As Integer = -1
        Public ContainFiles As New List(Of String)
        Public FlyToFile As String = ""
        Public FlyToPosition As Vector3 = New Vector3(0)

        Public RouteDirection As RouteDirections = RouteDirections.Horizontal
        Public RouteType As RouteTypes = RouteTypes.Land
        Public Visible As Integer = VisibleMode.Always
        Public Register As String = ""

        Dim T As Texture2D = Nothing

        Public Sub New(ByVal Name As String, ByVal ContainFiles() As String, ByVal PositionX As Integer, ByVal PositionY As Integer, ByVal RouteDirection As RouteDirections, ByVal RouteType As RouteTypes, Optional ByVal FlyToFile As String = "", Optional ByVal FlyToPosition As Vector3 = Nothing, Optional PlayerPositionX As Integer = -1, Optional PlayerPositionY As Integer = -1, Optional Visible As Integer = VisibleMode.Always, Optional Register As String = "")
            Me.Name = Name
            Me.PositionX = PositionX
            Me.PositionY = PositionY
            Me.RouteDirection = RouteDirection
            Me.RouteType = RouteType

            For Each file As String In ContainFiles
                Me.ContainFiles.Add(file.ToLower())
            Next
            If PlayerPositionX <> -1 Then
                Me.PlayerPositionX = PlayerPositionX
            Else
                Me.PlayerPositionX = PositionX
            End If
            If PlayerPositionY <> -1 Then
                Me.PlayerPositionY = PlayerPositionY
            Else
                Me.PlayerPositionY = PositionY
            End If
            Me.FlyToFile = FlyToFile
            Me.FlyToPosition = FlyToPosition

            Me.Visible = Visible
            Me.Register = Register
        End Sub

        Public Function getPosition() As Vector2
            Return New Vector2(Me.PositionX * MapScreen.RasterSize, Me.PositionY * MapScreen.RasterSize)
        End Function
        Public Function getPlayerPosition() As Vector2
            Return New Vector2(Me.PlayerPositionX * MapScreen.RasterSize, Me.PlayerPositionY * MapScreen.RasterSize)
        End Function

        Public Function getRectangle(ByVal offset As Vector2) As Rectangle
            Dim sizeX As Single = 1.0F
            Dim sizeY As Single = 1.0F

            Select Case Me.RouteDirection
                Case RouteDirections.Horizontal
                    sizeX = 1.25F
                    sizeY = 0.75F
                Case RouteDirections.Vertical
                    sizeX = 0.75F
                    sizeY = 1.25F
                Case RouteDirections.CurveDownLeft, RouteDirections.CurveDownRight, RouteDirections.CurveUpLeft, RouteDirections.CurveUpRight, RouteDirections.TUp, RouteDirections.TDown, RouteDirections.TRight, RouteDirections.TLeft
                    sizeX = 0.75F
                    sizeY = 0.75F
                Case RouteDirections.HorizontalConnection
                    sizeX = 1.0F
                    sizeY = 0.75F
                Case RouteDirections.VerticalConnection
                    sizeX = 0.75F
                    sizeY = 1.0F
                Case RouteDirections.HorizontalEndRight, RouteDirections.HorizontalEndLeft, RouteDirections.VerticalEndDown, RouteDirections.VerticalEndUp
                    sizeX = 0.75F
                    sizeY = 0.75F
            End Select

            Dim PositionOffset As New Vector2(((1 - sizeX) * MapScreen.RasterSize) / 2, ((1 - sizeY) * MapScreen.RasterSize) / 2)

            If Me.RouteDirection = RouteDirections.HorizontalConnection Then
                PositionOffset.X += 0.5F * MapScreen.RasterSize
            End If
            If Me.RouteDirection = RouteDirections.VerticalConnection Then
                PositionOffset.Y += 0.5F * MapScreen.RasterSize
            End If

            sizeX *= MapScreen.RasterSize
            sizeY *= MapScreen.RasterSize

            Return New Rectangle(CInt(Me.getPosition().X + PositionOffset.X + offset.X), CInt(Me.getPosition().Y + PositionOffset.Y + offset.Y), CInt(sizeX), CInt(sizeY))
        End Function

        Public Function getTexture(ByVal FullTexture As Texture2D, ByVal isSelected As Boolean) As Texture2D
            If Me.T Is Nothing Or isSelected = True Then
                Dim r As Rectangle

                Dim modX As Integer = 0
                If Me.RouteType = RouteTypes.Water Then
                    modX = 32
                End If
                If isSelected = True Then
                    modX = 64
                End If
                Dim y As Integer = 64

                Select Case Me.RouteDirection
                    Case RouteDirections.Horizontal, RouteDirections.HorizontalConnection
                        r = New Rectangle(0 + modX, 0 + y, 8, 8)
                    Case RouteDirections.TUp
                        r = New Rectangle(8 + modX, 0 + y, 8, 8)
                    Case RouteDirections.TDown
                        r = New Rectangle(8 + modX, 24 + y, 8, 8)
                    Case RouteDirections.TRight
                        r = New Rectangle(0 + modX, 24 + y, 8, 8)
                    Case RouteDirections.TLeft
                        r = New Rectangle(24 + modX, 16 + y, 8, 8)
                    Case RouteDirections.Vertical, RouteDirections.VerticalConnection
                        r = New Rectangle(16 + modX, 0 + y, 8, 8)
                    Case RouteDirections.HorizontalEndRight
                        r = New Rectangle(24 + modX, 0 + y, 8, 8)
                    Case RouteDirections.CurveUpLeft
                        r = New Rectangle(0 + modX, 8 + y, 8, 8)
                    Case RouteDirections.CurveDownRight
                        r = New Rectangle(8 + modX, 8 + y, 8, 8)
                    Case RouteDirections.CurveUpRight
                        r = New Rectangle(16 + modX, 8 + y, 8, 8)
                    Case RouteDirections.CurveDownLeft
                        r = New Rectangle(24 + modX, 8 + y, 8, 8)
                    Case RouteDirections.HorizontalEndLeft
                        r = New Rectangle(0 + modX, 16 + y, 8, 8)
                    Case RouteDirections.VerticalEndDown
                        r = New Rectangle(8 + modX, 16 + y, 8, 8)
                    Case RouteDirections.VerticalEndUp
                        r = New Rectangle(16 + modX, 16 + y, 8, 8)
                End Select

                Me.T = TextureManager.GetTexture(FullTexture, r)
            End If

            Return Me.T
        End Function

        Public Sub Click(ByVal flag() As Object)
            Select Case flag(0).ToString().ToLower()
                Case "fly"
                    If CanFlyTo(flag) = True Then
                        MapScreen.UseFly(FlyToFile, FlyToPosition, flag)
                    End If
            End Select
        End Sub

        Public Function CanFlyTo(ByVal flag() As Object) As Boolean
            If flag(0).ToString().ToLower() = "fly" Then
                If FlyToPosition <> Nothing And FlyToFile <> "" Then
                    If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(FlyToFile.ToLower()) = True Or GameController.IS_DEBUG_ACTIVE = True Or Core.Player.SandBoxMode = True Then
                        Return True
                    End If
                End If
            End If
            Return False
        End Function

    End Class

    Public Class Place

        Public Enum PlaceSizes
            Small
            Vertical
            Round
            Square
            VerticalBig
            Large
        End Enum

        Public Name As String = "???"
        Public ContainFiles As New List(Of String)
        Public PositionX As Integer
        Public PositionY As Integer
        Public PlayerPositionX As Integer = -1
        Public PlayerPositionY As Integer = -1
        Public PlaceSize As PlaceSizes
        Public Visible As Integer = VisibleMode.Always
        Public Register As String = ""

        Public FlyToFile As String = ""
        Public FlyToPosition As Vector3 = New Vector3(0)

        Dim T As Texture2D = Nothing

        Public Sub New(ByVal Name As String, ByVal ContainFiles() As String, ByVal PositionX As Integer, ByVal PositionY As Integer, ByVal PlaceSize As PlaceSizes, Optional ByVal FlyToFile As String = "", Optional ByVal FlyToPosition As Vector3 = Nothing, Optional PlayerPositionX As Integer = -1, Optional PlayerPositionY As Integer = -1, Optional Visible As Integer = VisibleMode.Always, Optional Register As String = "")
            Me.Name = Name
            Me.PositionX = PositionX
            Me.PositionY = PositionY
            Me.PlaceSize = PlaceSize

            For Each file As String In ContainFiles
                Me.ContainFiles.Add(file.ToLower())
            Next
            If PlayerPositionX <> -1 Then
                Me.PlayerPositionX = PlayerPositionX
            Else
                Me.PlayerPositionX = PositionX
            End If
            If PlayerPositionY <> -1 Then
                Me.PlayerPositionY = PlayerPositionY
            Else
                Me.PlayerPositionY = PositionY
            End If

            Me.FlyToFile = FlyToFile
            Me.FlyToPosition = FlyToPosition
            Me.Visible = Visible
            Me.Register = Register
        End Sub

        Public Function getPosition() As Vector2
            Return New Vector2(Me.PositionX * MapScreen.RasterSize, Me.PositionY * MapScreen.RasterSize)
        End Function
        Public Function getPlayerPosition() As Vector2
            Return New Vector2(Me.PlayerPositionX * MapScreen.RasterSize, Me.PlayerPositionY * MapScreen.RasterSize)
        End Function
        Public Function getRectangle(ByVal offset As Vector2) As Rectangle
            Dim sizeX As Single = 1.0F
            Dim sizeY As Single = 1.0F

            Select Case Me.PlaceSize
                Case PlaceSizes.Small
                    sizeX = 1.0F
                    sizeY = 1.0F
                Case PlaceSizes.Vertical
                    sizeX = 1.0F
                    sizeY = 2.0F
                Case PlaceSizes.Round
                    sizeX = 1.5F
                    sizeY = 1.5F
                Case PlaceSizes.Square
                    sizeX = 2.0F
                    sizeY = 2.0F
                Case PlaceSizes.VerticalBig
                    sizeX = 1.5F
                    sizeY = 2.5F
                Case PlaceSizes.Large
                    sizeX = 3.5F
                    sizeY = 2.5F
            End Select

            Dim PositionOffset As New Vector2(((1 - sizeX) * MapScreen.RasterSize) / 2, ((1 - sizeY) * MapScreen.RasterSize) / 2)

            If Me.PlaceSize = PlaceSizes.Vertical Then
                PositionOffset.Y += 0.5F * MapScreen.RasterSize
            End If

            sizeX *= MapScreen.RasterSize
            sizeY *= MapScreen.RasterSize

            Return New Rectangle(CInt(Me.getPosition().X + PositionOffset.X + offset.X), CInt(Me.getPosition().Y + PositionOffset.Y + offset.Y), CInt(sizeX), CInt(sizeY))
        End Function

        Public Function getTexture(ByVal FullTexture As Texture2D, ByVal isSelected As Boolean) As Texture2D
            If Me.T Is Nothing Or isSelected = True Then
                Dim r As Rectangle

                Dim modX As Integer = 0
                If isSelected = True Then
                    modX = 56
                End If
                Dim y As Integer = 96

                Select Case Me.PlaceSize
                    Case PlaceSizes.Small
                        r = New Rectangle(12 + modX, 20 + y, 8, 8)
                    Case PlaceSizes.Vertical
                        r = New Rectangle(40 + modX, 16 + y, 8, 16)
                    Case PlaceSizes.Round
                        r = New Rectangle(0 + modX, 20 + y, 12, 12)
                    Case PlaceSizes.Square
                        r = New Rectangle(40 + modX, 0 + y, 16, 16)
                    Case PlaceSizes.VerticalBig
                        r = New Rectangle(0 + modX, 0 + y, 12, 20)
                    Case PlaceSizes.Large
                        r = New Rectangle(12 + modX, 0 + y, 28, 20)
                End Select
                Me.T = TextureManager.GetTexture(FullTexture, r)
            End If

            Return Me.T
        End Function

        Public Sub Click(ByVal flag() As Object)
            Select Case flag(0).ToString().ToLower()
                Case "fly"
                    If CanFlyTo(flag) = True Then
                        MapScreen.UseFly(FlyToFile, FlyToPosition, flag)
                    End If
            End Select
        End Sub

        Public Function CanFlyTo(ByVal flag() As Object) As Boolean
            If flag(0).ToString().ToLower() = "fly" Then
                If FlyToPosition <> Nothing And FlyToFile <> "" Then
                    Dim flytomap As Boolean = False
                    For Each map In ContainFiles
                        If Core.Player.VisitedMaps.ToLower().Split(CChar(",")).Contains(map.ToLower()) = True Then
                            flytomap = True
                            Exit For
                        End If
                    Next
                    If flytomap = True Or GameController.IS_DEBUG_ACTIVE = True Or Core.Player.SandBoxMode = True Then
                        Return True
                    End If
                End If
            End If
            Return False
        End Function

    End Class

    Public Class Roaming

        Public ID As String
        Public Name As String
        Public Location As String
        Public PositionX As Integer
        Public PositionY As Integer
        Public Distance As Double
        Public Species As Pokemon

        Dim T As Texture2D = Nothing

        Public Sub New(ByVal ID As String, ByVal PositionX As Integer, ByVal PositionY As Integer, ByVal Location As String)
            Me.ID = ID
            Dim PokemonID As String = ID
            Dim PokemonAddition As String = "xXx"
            If PokemonID.Contains("_") Then
                PokemonAddition = PokemonForms.GetAdditionalValueFromDataFile(ID)
                PokemonID = ID.GetSplit(0, "_")
            End If
            If PokemonID.Contains(";") Then
                PokemonAddition = ID.GetSplit(1, ";")
                PokemonID = ID.GetSplit(0, ";")
            End If
            Me.Name = Pokemon.GetPokemonByID(CInt(PokemonID), PokemonAddition).GetName
            Me.Species = Pokemon.GetPokemonByID(CInt(PokemonID), PokemonAddition)
            Me.PositionX = PositionX
            Me.PositionY = PositionY
            Me.Location = Location
            Me.Distance = Math.Pow(Math.Pow(PositionX, 2) + Math.Pow(PositionY, 2), 0.5)
        End Sub

        Public Function getPosition() As Vector2
            Return New Vector2(Me.PositionX * MapScreen.RasterSize, Me.PositionY * MapScreen.RasterSize)
        End Function

        Public Function getRectangle(ByVal offset As Vector2) As Rectangle
            Dim sizeX As Integer = 1
            Dim sizeY As Integer = 1

            sizeX *= MapScreen.RasterSize
            sizeY *= MapScreen.RasterSize

            Return New Rectangle(CInt(Me.getPosition().X + offset.X), CInt(Me.getPosition().Y + offset.Y), sizeX, sizeY)
        End Function

        'Public Function getTexture() As Texture2D
        'Dim Texture As Texture2D = TextureManager.GetTexture("GUI\PokemonMenu")
        'Dim IndexX As Integer = 0
        'Dim IndexY As Integer = 0
        'Dim SizeX As Integer = 32
        'Dim SizeY As Integer = 32
        '
        'IndexY = CInt(Math.Floor(ID / 33))
        'IndexX = (ID - (IndexY * 32)) - 1
        '
        'T = TextureManager.GetTexture(Texture, New Rectangle(IndexX * 32, IndexY * 32, SizeX, SizeY))
        '
        'Return T
        'End Function

        Public Function getTexture() As Texture2D
            Dim v As Vector2 = PokemonForms.GetMenuImagePosition(Species)
            Dim s As Size = PokemonForms.GetMenuImageSize(Species)
            Dim sheet As String = PokemonForms.GetSheetName(Species)

            Return P3D.TextureManager.GetTexture("GUI\PokemonMenu\" & sheet, New Rectangle(CInt(v.X) * 32, CInt(v.Y) * 32, s.Width, s.Height), "")
        End Function

        Public Function getSkipIndex() As Integer
            Select Case Location
                Case "Route 31", "Route 37", "Route 42"
                    Return 0
                Case "Route 29", "Route 30", "Route 33", "Route 34", "Route 35", "Route 36", "Route 38", "Route 39", "Route 44"
                    Return 1
                Case "Route 32", "Route 45"
                    Return 2
                Case Else
                    Return 0
            End Select
        End Function
    End Class
End Class