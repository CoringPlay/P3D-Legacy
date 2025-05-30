﻿Namespace GameJolt

    Public Class PokegearScreen

        Inherits Screen

        Public Shared TradeRequestData As Integer = -1 ' This data gets set to the network ID of the user that requests a trade.
        Public Shared BattleRequestData As Integer = -1 ' This data gets set to the network ID of the user that requests a battle.

        Enum MenuScreens As Integer
            Main = 0
            PSS = 1
            UserView = 2
            PhoneList = 3
            Frontier = 4
            MiniMap = 5
            Radio = 6
            TradeRequest = 7
            BattleRequest = 8
        End Enum

        Enum EntryModes As Integer
            MainMenu
            DisplayUser
            TradeRequest
            BattleRequest
        End Enum

        Dim FunctionList As New List(Of String)

        Dim width As Integer = 720
        Dim heigth As Integer = 400

        Dim Cursors() As Integer
        Public menuIndex As MenuScreens = MenuScreens.Main

        Public PublicKeys As String = ""

        Public Sub New(ByVal currentScreen As Screen, ByVal EntryMode As EntryModes, ByVal Data() As Object)
            Me.Identification = Identifications.PokegearScreen
            Me.PreScreen = currentScreen

            Me.MouseVisible = True

            If Core.Player.IsGameJoltSave = True Then
                Me.UserBanned = LogInScreen.UserBanned(Core.GameJoltSave.GameJoltID)
                Dim APICall As New APICall(AddressOf GotPublicKeys)
                APICall.GetKeys(False, "saveStorageV" & GameJolt.GamejoltSave.VERSION & "|*|*")
            End If

            If Me.UserBanned = False Then
                FunctionList.Add("PSS")
                If API.LoggedIn = True And Core.Player.IsGameJoltSave = True And Core.Player.Pokemons.Count > 0 Then
                    FunctionList.Add("Battle Spot")
                End If
                If (ActionScript.IsRegistered("pokegear_card_GTS") = True Or GameController.IS_DEBUG_ACTIVE = True) = True And API.LoggedIn = True And Core.Player.IsGameJoltSave = True Then
                    FunctionList.Add("GTS")
                End If
                If API.LoggedIn = True And Core.Player.IsGameJoltSave = True And Core.Player.Pokemons.Count > 0 Then
                    FunctionList.Add("Wondertrade")
                End If
            End If

            If ActionScript.IsRegistered("pokegear_remove_phone") = False Then
                FunctionList.Add("Phone")
            End If
            If ActionScript.IsRegistered("pokegear_card_radio") = True Or GameController.IS_DEBUG_ACTIVE = True Then
                FunctionList.Add("Radio")
            End If
            If ActionScript.IsRegistered("pokegear_remove_worldmap") = False Then
                FunctionList.Add("Worldmap")
            End If
            If ActionScript.IsRegistered("pokegear_card_minimap") = True Or GameController.IS_DEBUG_ACTIVE = True Then
                FunctionList.Add("Minimap")
            End If

            If PlayerStatistics.CountStatistics() > 0 Then
                FunctionList.Add("Statistics")
            End If

            If ActionScript.IsRegistered("pokegear_card_frontier") = True Or GameController.IS_DEBUG_ACTIVE = True Then
                FunctionList.Add("Frontier")
            End If

            Cursors = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}

            Select Case EntryMode
                Case EntryModes.MainMenu
                    SoundManager.PlaySound("Pokegear\pokegear_on")
                    Me.menuIndex = Player.Temp.LastPokegearPage
                Case EntryModes.DisplayUser
                    InitializeUserView(Data)
                Case EntryModes.TradeRequest
                    InitializeTradeRequest(Data)
                Case EntryModes.BattleRequest
                    InitializeBattleRequest(Data)
            End Select
        End Sub

        Private Sub InitializeUserView(ByVal Data() As Object)
            ' Data: NetworkID, GameJoltID, Name, Sprite

            UserEmblem = Nothing
            UserSprite = CType(Data(3), Texture2D)
            UserName = CStr(Data(2))
            UserOrigin = "Server"
            UserNetworkID = CInt(Data(0))

            menuIndex = MenuScreens.UserView
            UserViewPreMenu = MenuScreens.Main
        End Sub

        Private Sub InitializeTradeRequest(ByVal Data() As Object)
            ' Data: NetworkID of the requester, GameJoltID

            menuIndex = MenuScreens.TradeRequest
            Me.TradeRequestNetworkID = CInt(Data(0))
            Me.TradeRequestGameJoltID = CStr(Data(1))
            SoundManager.PlaySound("Use_Item", False)
            TradeRequestData = -1
        End Sub

        Private Sub InitializeBattleRequest(ByVal Data() As Object)
            ' Data: NetworkID of the requester, GameJoltID

            menuIndex = MenuScreens.BattleRequest
            Me.BattleRequestNetworkID = CInt(Data(0))
            Me.BattleRequestGameJoltID = CStr(Data(1))
            SoundManager.PlaySound("Use_Item", False)
            BattleRequestData = -1
        End Sub

        Private Sub GotPublicKeys(ByVal result As String)
            Me.PublicKeys = result
        End Sub

        Public Overrides Sub Draw()
            Me.PreScreen.Draw()

            DrawMain()

            Select Case Me.menuIndex
                Case MenuScreens.Main
                    DrawMainMenu()
                Case MenuScreens.PSS
                    DrawPSS()
                Case MenuScreens.UserView
                    DrawUserView()
                Case MenuScreens.PhoneList
                    DrawPhone()
                Case MenuScreens.Frontier
                    DrawFrontier()
                Case MenuScreens.MiniMap
                    DrawMinimap()
                Case MenuScreens.Radio
                    DrawRadio()
                Case MenuScreens.TradeRequest
                    DrawTradeRequest()
                Case MenuScreens.BattleRequest
                    DrawBattleRequest()
            End Select
        End Sub

        Public Overrides Sub Update()
            Select Case Me.menuIndex
                Case MenuScreens.Main
                    UpdateMainMenu()
                Case MenuScreens.PSS
                    UpdatePSS()
                Case MenuScreens.UserView
                    UpdateUserView()
                Case MenuScreens.PhoneList
                    UpdatePhone()
                Case MenuScreens.Frontier
                    UpdateFrontier()
                Case MenuScreens.MiniMap
                    UpdateMinimap()
                Case MenuScreens.Radio
                    UpdateRadio()
                Case MenuScreens.TradeRequest
                    UpdateTradeRequest()
                Case MenuScreens.BattleRequest
                    UpdateBattleRequest()
            End Select

            If Me.menuIndex <> MenuScreens.TradeRequest Then
                If KeyBoardHandler.KeyPressed(KeyBindings.SpecialKey) = True Or ControllerHandler.ButtonPressed(Buttons.Back) = True Then
                    If Me.menuIndex <> MenuScreens.UserView Then Player.Temp.LastPokegearPage = Me.menuIndex
                    SoundManager.PlaySound("Pokegear\pokegear_off")
                    Core.SetScreen(Me.PreScreen)
                End If
            End If
        End Sub

        Private Sub DrawMain()
            Dim startPos As Vector2 = GetStartPosition()

            Canvas.DrawRectangle(New Rectangle(CInt(startPos.X) - 1, CInt(startPos.Y) - 1, width + 2, heigth + 2), New Color(100, 100, 100))

            Canvas.DrawGradient(New Rectangle(CInt(startPos.X), CInt(startPos.Y), width, 30), New Color(11, 27, 61), New Color(21, 40, 96), False, -1)
            Canvas.DrawGradient(New Rectangle(CInt(startPos.X), CInt(startPos.Y) + 30, width, heigth - 30), New Color(225, 220, 94), New Color(210, 172, 73), False, -1)

            Dim t As String = TimeHelpers.GetDisplayTime(Date.Now, True)
            Dim t2 As String = "Pokégear"

            Core.SpriteBatch.DrawString(FontManager.MainFont, t, New Vector2(startPos.X + width - FontManager.MainFont.MeasureString(t).X - 5, startPos.Y + 4), Color.White)
            Core.SpriteBatch.DrawString(FontManager.MainFont, t2, New Vector2(startPos.X + 5, startPos.Y + 4), Color.White)

            If API.LoggedIn = True Then
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 10 + FontManager.MainFont.MeasureString(t2).X), CInt(startPos.Y + 8), 16, 16), New Rectangle(80, 112, 16, 16), Color.White)
            End If
            If ConnectScreen.Connected = True Then
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 34 + FontManager.MainFont.MeasureString(t2).X), CInt(startPos.Y + 8), 16, 16), New Rectangle(112, 112, 16, 16), Color.White)
            End If
            Select Case World.GetTime
                Case World.DayTimes.Night
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + width - FontManager.MainFont.MeasureString(t).X - 34), CInt(startPos.Y + 6), 16, 16), New Rectangle(64, 112, 8, 8), Color.White)
                Case World.DayTimes.Morning
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + width - FontManager.MainFont.MeasureString(t).X - 34), CInt(startPos.Y + 6), 16, 16), New Rectangle(72, 112, 8, 8), Color.White)
                Case World.DayTimes.Day
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + width - FontManager.MainFont.MeasureString(t).X - 34), CInt(startPos.Y + 6), 16, 16), New Rectangle(64, 120, 8, 8), Color.White)
                Case World.DayTimes.Evening
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + width - FontManager.MainFont.MeasureString(t).X - 34), CInt(startPos.Y + 6), 16, 16), New Rectangle(72, 120, 8, 8), Color.White)
            End Select
        End Sub

#Region "MainMenu"

        Private Sub DrawMainMenu()
            Dim startPos As Vector2 = GetStartPosition()

            Dim x As Integer = 0
            Dim y As Integer = 0

            For i = 0 To Me.FunctionList.Count - 1
                Dim f As String = Me.FunctionList(i)
                Dim t As Texture2D = Nothing
                Dim displayText As String = ""

                Select Case f
                    Case "PSS"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(64, 32, 32, 32), "")
                        displayText = "PSS"
                    Case "Wondertrade"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(0, 64, 32, 32), "")
                        displayText = "Wondertrade"
                    Case "Phone"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(96, 32, 32, 32), "")
                        displayText = "Phone"
                    Case "Radio"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(32, 32, 32, 32), "")
                        displayText = "Radio"
                    Case "GTS"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(64, 0, 32, 32), "")
                        displayText = "GTS"
                    Case "Frontier"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(32, 64, 32, 32), "")
                        displayText = "Frontier"
                    Case "Worldmap"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(64, 64, 32, 32), "")
                        displayText = "Worldmap"
                    Case "Minimap"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(96, 64, 32, 32), "")
                        displayText = "Minimap"
                    Case "Battle Spot"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(32, 96, 32, 32), "")
                        displayText = "BattleSpot"
                    Case "Statistics"
                        t = TextureManager.GetTexture("GUI\Menus\pokegear", New Rectangle(32, 0, 32, 32), "")
                        displayText = "Statistics"
                End Select

                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 152) + 80), CInt(startPos.Y + 80 + (y * 100)), 64, 64), New Rectangle(0, 0, 32, 32), Color.White)
                Core.SpriteBatch.Draw(t, New Rectangle(CInt(startPos.X + (x * 152) + 80), CInt(startPos.Y + 80 + (y * 100)), 64, 64), New Rectangle(0, 0, 32, 32), Color.White)
                Core.SpriteBatch.DrawString(FontManager.MainFont, displayText, New Vector2(CInt(startPos.X + (x * 152) + 80) + 32 - FontManager.MainFont.MeasureString(displayText).X / 2.0F, CInt(startPos.Y + 152 + (y * 100))), Color.Black)

                If Cursors(0) = i Then
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 152) + 80), CInt(startPos.Y + 80 + (y * 100)), 64, 64), New Rectangle(0, 32, 32, 32), Color.White)
                End If

                x += 1
                While x > 3
                    x -= 4
                    y += 1
                End While
            Next
        End Sub

        Private Sub UpdateMainMenu()
            If Controls.Right(True, True, True, True, True, True) = True Then
                Cursors(0) += 1
            End If
            If Controls.Left(True, True, True, True, True, True) = True Then
                Cursors(0) -= 1
            End If
            If Controls.Up(True, True, False, True, True, True) = True Then
                Cursors(0) -= 4
            End If
            If Controls.Down(True, True, False, True, True, True) = True Then
                Cursors(0) += 4
            End If

            Cursors(0) = Cursors(0).Clamp(0, FunctionList.Count - 1)

            If Controls.Accept(True, False, False) = True Then
                Dim pressedIndex As Integer = -1
                Dim startPos As Vector2 = GetStartPosition()
                For x = 0 To 3
                    For y = 0 To 2
                        Dim r As New Rectangle(CInt(startPos.X + (x * 152) + 80), CInt(startPos.Y + 80 + (y * 100)), 64, 64)
                        If r.Contains(MouseHandler.MousePosition) = True Then
                            pressedIndex = x + y * 4
                        End If
                    Next
                Next
                If pressedIndex > -1 Then
                    If FunctionList.Count - 1 >= pressedIndex Then
                        If Cursors(0) = pressedIndex Then
                            SoundManager.PlaySound("select")
                            PressedMainMenuButton()
                        Else
                            Cursors(0) = pressedIndex
                        End If
                    End If
                End If
            End If

            If Controls.Accept(False, True, True) = True Then
                SoundManager.PlaySound("select")
                PressedMainMenuButton()
            End If

            If Controls.Dismiss(True, True) = True Then
                SoundManager.PlaySound("Pokegear\pokegear_off")
                Core.SetScreen(Me.PreScreen)
            End If
        End Sub

        Private Sub PressedMainMenuButton()
            Select Case FunctionList(Cursors(0))
                Case "PSS"
                    Me.menuIndex = MenuScreens.PSS
                Case "Battle Spot"
                    Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New RegisterBattleScreen(Core.CurrentScreen), Color.White, False))
                Case "Phone"
                    Me.menuIndex = MenuScreens.PhoneList
                Case "GTS"
                    Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New GTSMainScreen(Core.CurrentScreen), Color.White, False))
                Case "Frontier"
                    Me.menuIndex = MenuScreens.Frontier
                Case "Worldmap"
                    Dim argument As String = Screen.Level.CurrentRegion
                    If argument.Contains(",") = True Then
                        Dim regions As List(Of String) = argument.Split(CChar(",")).ToList()
                        Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New MapScreen(Core.CurrentScreen, regions, 0, {"view"}), Color.White, False))
                    Else
                        Dim startRegion As String = argument
                        Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New MapScreen(Core.CurrentScreen, startRegion, {"view"}), Color.White, False))
                    End If
                Case "Minimap"
                    Me.menuIndex = MenuScreens.MiniMap
                Case "Radio"
                    Me.menuIndex = MenuScreens.Radio
                Case "Wondertrade"
                    Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New WonderTradeScreen(Core.CurrentScreen), Color.White, False))
                Case "Statistics"
                    Core.SetScreen(New TransitionScreen(Core.CurrentScreen, New StatisticsScreen(Core.CurrentScreen), Color.White, False))
            End Select
        End Sub

#End Region

#Region "PSS"

        Dim PSSmenuIndex As Integer = 0

        Dim UserBanned As Boolean = False

        Private Sub DrawPSS()
            Select Case PSSmenuIndex
                Case 0
                    DrawRanklist()
                Case 1
                    DrawFriendList()
                Case 2
                    DrawLocalList()
            End Select
        End Sub

        Private Sub UpdatePSS()
            Select Case Me.PSSmenuIndex
                Case 0
                    UpdateRanklist()
                Case 1
                    UpdateFriendList()
                Case 2
                    UpdateLocalList()
            End Select

            If Controls.Right(True, True, False, True, True, True) = True Then
                PSSmenuIndex += 1
            End If
            If Controls.Left(True, True, False, True, True, True) = True Then
                PSSmenuIndex -= 1
            End If
            Dim startPos As Vector2 = GetStartPosition()
            Dim recRank As New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 184, 32)
            Dim recFriends As New Rectangle(CInt(startPos.X + 40 + 200), CInt(startPos.Y + 46), 184, 32)
            Dim recLocal As New Rectangle(CInt(startPos.X + 40 + 200 + 200), CInt(startPos.Y + 46), 184, 32)
            If Controls.Accept(True, False, False) Then
                If recRank.Contains(MouseHandler.MousePosition) Then
                    PSSmenuIndex = 0
                End If
                If recFriends.Contains(MouseHandler.MousePosition) Then
                    PSSmenuIndex = 1
                End If
                If recLocal.Contains(MouseHandler.MousePosition) Then
                    PSSmenuIndex = 2
                End If
            End If

            If PSSmenuIndex > 2 Then
                PSSmenuIndex = 0
            End If
            If PSSmenuIndex < 0 Then
                PSSmenuIndex = 2
            End If

            If Controls.Dismiss(True, True, True) = True Then
                Me.menuIndex = MenuScreens.Main
            End If
        End Sub

#Region "Ranklist"

        Dim RankListScroll As Integer = 0
        Dim RankListSelect As Integer = 0
        Dim OwnRank As Integer = 0
        Dim RankingList As New List(Of Emblem)
        Dim InitializedRanklist As Boolean = False

        Private Sub DrawRanklist()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Ranklist", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Friendlist", New Vector2(CInt(startPos.X + 48 + 200), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Passby", New Vector2(CInt(startPos.X + 48 + 200 + 200), CInt(startPos.Y + 48)), Color.Black)

            If Core.Player.IsGameJoltSave = True Then
                ' Draw own information:
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80), 480, 32), New Rectangle(102, 112, 4, 16), Color.White)
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 80), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

                Dim ownE As New Emblem(Core.Player.Name, Core.GameJoltSave.GameJoltID, Core.GameJoltSave.Points, Core.GameJoltSave.Gender, Core.GameJoltSave.Emblem)

                Dim ownFrameSize As New Size(CInt(ownE.SpriteTexture.Width / 3), CInt(ownE.SpriteTexture.Height / 4))
                Dim ownFrameScale As Single = 1.0F
                If ownE.SpriteTexture.Width = ownE.SpriteTexture.Height / 2 Then
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 2)
                ElseIf ownE.SpriteTexture.Width = ownE.SpriteTexture.Height Then
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 4)
                Else
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 3)
                End If
                If ownFrameSize.Width > 32 Then
                    ownFrameScale = CSng(32 / ownFrameSize.Width)
                End If
                If ownFrameSize.Width <= 16 Then
                    ownFrameScale = 2.0F
                End If
                Core.SpriteBatch.Draw(ownE.SpriteTexture, New Rectangle(CInt(startPos.X + 64 - (ownFrameSize.Width * ownFrameScale / 2)), CInt(startPos.Y + 96 - (ownFrameSize.Height * ownFrameScale / 2)), CInt(ownFrameSize.Width * ownFrameScale), CInt(ownFrameSize.Height * ownFrameScale)), New Rectangle(0, ownFrameSize.Height * 2, ownFrameSize.Width, ownFrameSize.Height), Color.White)
                Core.SpriteBatch.DrawString(FontManager.MainFont, ownE.Username, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 80)), Color.Black)

                If OwnRank > -1 Then
                    Core.SpriteBatch.DrawString(FontManager.MainFont, "Rank " & OwnRank.ToString(), New Vector2(CInt(startPos.X + 332), CInt(startPos.Y + 80)), Color.Black)
                End If

                If UserBanned = False Then
                    If PublicKeys <> "" Then
                        For i = 0 To 8
                            Dim index As Integer = i + RankListScroll
                            Dim e As Emblem = Nothing

                            If index <= RankingList.Count - 1 Then
                                e = RankingList(index)

                                If Not e Is Nothing Then
                                    Dim eff As SpriteEffects = SpriteEffects.None
                                    If index = RankListSelect Then
                                        eff = SpriteEffects.FlipVertically
                                    End If

                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + 36 + i * 36), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80 + 36 + i * 36), 480, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 80 + 36 + i * 36), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                                    If e.DoneLoading = True Then
                                        Dim frameSize As New Size(CInt(e.SpriteTexture.Width / 3), CInt(e.SpriteTexture.Height / 4))
                                        Dim frameScale As Single = 1.0F
                                        If e.SpriteTexture.Width = e.SpriteTexture.Height / 2 Then
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 2)
                                        ElseIf e.SpriteTexture.Width = e.SpriteTexture.Height Then
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 4)
                                        Else
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 3)
                                        End If
                                        If frameSize.Width > 32 Then
                                            frameScale = CSng(32 / frameSize.Width)
                                        End If
                                        If frameSize.Width <= 16 Then
                                            frameScale = 2.0F
                                        End If
                                        Core.SpriteBatch.Draw(e.SpriteTexture, New Rectangle(CInt(startPos.X + 64 - (frameSize.Width * frameScale / 2)), CInt(startPos.Y + 96 - (frameSize.Height * frameScale / 2) + 36 + i * 36), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)
                                        Core.SpriteBatch.DrawString(FontManager.MainFont, e.Username, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Rank " & (index + 1).ToString() & " (" & e.Points.ToString() & ")", New Vector2(CInt(startPos.X + 332), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                    Else
                                        If e.startedLoading = False Then
                                            e.StartLoading(e.Username)
                                        End If

                                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Loading" & LoadingDots.Dots, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                    End If
                                End If
                            End If
                        Next
                        Canvas.DrawScrollBar(New Vector2(startPos.X + 570, startPos.Y + 88), 100, 9, RankListScroll, New Size(4, 300), False, New Color(252, 196, 68), New Color(217, 120, 18))
                    Else
                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Loading" & LoadingDots.Dots, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 80 + 36)), Color.Black)
                    End If
                End If
            Else
                Core.SpriteBatch.DrawString(FontManager.MainFont, "You are not logged in with a GameJolt profile.", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 88)), Color.Black)
            End If
        End Sub

        Private Sub UpdateRanklist()
            If InitializedRanklist = False Then
                InitalizeRanklist()
            Else
                If RankingList.Count > 0 Then
                    Dim c As Integer = 1
                    If Controls.ShiftDown() = True Then
                        c = 5
                    End If

                    If Controls.Up(True, True, False, True, True, True) = True Then
                        If RankListSelect = RankListScroll Then
                            RankListScroll -= c
                        End If
                        RankListSelect -= c
                    End If
                    If Controls.Down(True, True, False, True, True, True) = True Then
                        If RankListSelect = RankListScroll + 8 Then
                            RankListScroll += c
                        End If
                        RankListSelect += c
                    End If
                    If Controls.Up(True, False, True, False, False, False) = True Then
                        RankListScroll -= c
                        RankListSelect -= c
                    End If
                    If Controls.Down(True, False, True, False, False, False) = True Then
                        RankListScroll += c
                        RankListSelect += c
                    End If

                    If Controls.Accept(True, False, False) = True Then
                        Dim startPos As Vector2 = GetStartPosition()
                        For i = 0 To 8
                            Dim r As New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + 36 + i * 36), 510, 32)
                            If r.Contains(MouseHandler.MousePosition) Then
                                If RankListSelect = i + RankListScroll Then
                                    SoundManager.PlaySound("select")
                                    PSSRanklistDisplayUser()
                                Else
                                    RankListSelect = i + RankListScroll
                                End If
                                Exit For
                            End If
                        Next
                    End If

                    If RankListScroll > RankListSelect Then
                        RankListScroll = RankListSelect
                    End If
                    If RankListScroll + 8 < RankListSelect Then
                        RankListScroll = RankListSelect - 8
                    End If

                    RankListScroll = RankListScroll.Clamp(0, RankingList.Count - 9)
                    RankListSelect = RankListSelect.Clamp(0, RankingList.Count - 1)

                    If Controls.Accept(False, True, True) = True Then
                        SoundManager.PlaySound("select")
                        PSSRanklistDisplayUser()
                    End If
                End If
            End If
        End Sub

        Private Sub InitalizeRanklist()
            If Me.PublicKeys <> "" Then
                Me.RankingList.Clear()
                InitializedRanklist = True

                If Core.Player.IsGameJoltSave = True And UserBanned = False Then
                    Dim APICall As New APICall(AddressOf GotDataRanklist)

                    APICall.FetchTable(100, "14908")

                    Dim APICallOwnRank As New APICall(AddressOf GotOwnRank)
                    APICallOwnRank.FetchUserRank("14908", Core.GameJoltSave.Points)
                End If
            End If
        End Sub

        Private Sub GotDataRanklist(ByVal result As String)
            Dim list As List(Of API.JoltValue) = API.HandleData(result)

            For Each Item As API.JoltValue In list
                If Item.Name.ToLower() = "user" Then
                    RankingList.Add(New Emblem(Item.Value, PublicKeys, False))
                End If
            Next
        End Sub

        Private Sub GotOwnRank(ByVal result As String)
            Dim list As List(Of API.JoltValue) = API.HandleData(result)

            For Each Item As API.JoltValue In list
                If Item.Name.ToLower() = "rank" Then
                    OwnRank = CInt(Item.Value)
                End If
            Next
        End Sub

        Private Sub PSSRanklistDisplayUser()
            UserEmblem = RankingList(RankListSelect)
            UserSprite = UserEmblem.SpriteTexture
            UserName = UserEmblem.Username
            UserOrigin = "GJ"
            For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                If p.GameJoltId <> "" Then
                    If UserEmblem.GameJoltID = p.GameJoltId Then
                        UserOrigin &= "|Server"
                        UserNetworkID = p.ServersID
                    End If
                End If
            Next
            menuIndex = MenuScreens.UserView
            UserViewPreMenu = MenuScreens.PSS
        End Sub

#End Region

#Region "Friends"

        Dim InitializedFriends As Boolean = False
        Dim FriendList As New List(Of Emblem)
        Dim FriendListScroll As Integer = 0
        Dim FriendListSelect As Integer = 0

        Private Sub DrawFriendList()
            Dim startPos As Vector2 = GetStartPosition()
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Ranklist", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Friendlist", New Vector2(CInt(startPos.X + 48 + 200), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Passby", New Vector2(CInt(startPos.X + 48 + 200 + 200), CInt(startPos.Y + 48)), Color.Black)

            If Core.Player.IsGameJoltSave = True Then
                ' Draw own information:
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80), 480, 32), New Rectangle(102, 112, 4, 16), Color.White)
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 80), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

                Dim ownE As New Emblem(Core.Player.Name, Core.GameJoltSave.GameJoltID, Core.GameJoltSave.Points, Core.GameJoltSave.Gender, Core.GameJoltSave.Emblem)

                Dim ownFrameSize As New Size(CInt(ownE.SpriteTexture.Width / 3), CInt(ownE.SpriteTexture.Height / 4))
                Dim ownFrameScale As Single = 1.0F
                If ownE.SpriteTexture.Width = ownE.SpriteTexture.Height / 2 Then
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 2)
                ElseIf ownE.SpriteTexture.Width = ownE.SpriteTexture.Height Then
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 4)
                Else
                    ownFrameSize.Width = CInt(ownE.SpriteTexture.Width / 3)
                End If
                If ownFrameSize.Width > 32 Then
                    ownFrameScale = CSng(32 / ownFrameSize.Width)
                End If
                If ownFrameSize.Width <= 16 Then
                    ownFrameScale = 2.0F
                End If

                Core.SpriteBatch.Draw(ownE.SpriteTexture, New Rectangle(CInt(startPos.X + 64 - (ownFrameSize.Width * ownFrameScale / 2)), CInt(startPos.Y + 96 - (ownFrameSize.Height * ownFrameScale / 2)), CInt(ownFrameSize.Width * ownFrameScale), CInt(ownFrameSize.Height * ownFrameScale)), New Rectangle(0, ownFrameSize.Height * 2, ownFrameSize.Width, ownFrameSize.Height), Color.White)
                Core.SpriteBatch.DrawString(FontManager.MainFont, ownE.Username, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 80)), Color.Black)

                If OwnRank > -1 Then
                    Core.SpriteBatch.DrawString(FontManager.MainFont, "Rank " & OwnRank.ToString(), New Vector2(CInt(startPos.X + 332), CInt(startPos.Y + 80)), Color.Black)
                End If

                If UserBanned = False Then
                    If PublicKeys <> "" Then
                        For i = 0 To 8
                            Dim index As Integer = i + FriendListScroll
                            Dim e As Emblem = Nothing

                            If index <= FriendList.Count - 1 Then
                                e = FriendList(index)

                                If Not e Is Nothing Then
                                    Dim eff As SpriteEffects = SpriteEffects.None
                                    If index = FriendListSelect Then
                                        eff = SpriteEffects.FlipVertically
                                    End If

                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + 36 + i * 36), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80 + 36 + i * 36), 480, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 80 + 36 + i * 36), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                                    If e.DoneLoading = True Then
                                        Dim frameSize As New Size(CInt(e.SpriteTexture.Width / 3), CInt(e.SpriteTexture.Height / 4))
                                        Dim frameScale As Single = 1.0F
                                        If e.SpriteTexture.Width = e.SpriteTexture.Height / 2 Then
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 2)
                                        ElseIf e.SpriteTexture.Width = e.SpriteTexture.Height Then
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 4)
                                        Else
                                            frameSize.Width = CInt(e.SpriteTexture.Width / 3)
                                        End If
                                        If frameSize.Width > 32 Then
                                            frameScale = CSng(32 / frameSize.Width)
                                        End If
                                        If frameSize.Width <= 16 Then
                                            frameScale = 2.0F
                                        End If
                                        Core.SpriteBatch.Draw(e.SpriteTexture, New Rectangle(CInt(startPos.X + 64 - (frameSize.Width * frameScale / 2)), CInt(startPos.Y + 80 + 36 - (frameSize.Height * frameScale / 2) + i * 36), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)
                                        Core.SpriteBatch.DrawString(FontManager.MainFont, e.Username, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Level " & Emblem.GetPlayerLevel(e.Points) & " (" & e.Points.ToString() & ")", New Vector2(CInt(startPos.X + 332), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                    Else
                                        If e.startedLoading = False Then
                                            e.StartLoading(e.Username)
                                        End If

                                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Loading" & LoadingDots.Dots, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 80 + 36 + i * 36)), Color.Black)
                                    End If
                                End If
                            End If
                        Next
                        Canvas.DrawScrollBar(New Vector2(startPos.X + 570, startPos.Y + 88), FriendList.Count, 9, FriendListScroll, New Size(4, 300), False, New Color(252, 196, 68), New Color(217, 120, 18))
                    Else
                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Loading" & LoadingDots.Dots, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 80 + 36)), Color.Black)
                    End If
                End If
            Else
                Core.SpriteBatch.DrawString(FontManager.MainFont, "You are not logged in with a GameJolt profile.", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 88)), Color.Black)
            End If
        End Sub

        Private Sub UpdateFriendList()
            If InitializedFriends = False Then
                InitializeFriends()
            Else
                If FriendList.Count > 0 Then
                    Dim c As Integer = 1
                    If Controls.ShiftDown() = True Then
                        c = 5
                    End If

                    If Controls.Up(True, True, False, True, True, True) = True Then
                        If FriendListSelect = FriendListScroll Then
                            FriendListScroll -= c
                        End If
                        FriendListSelect -= c
                    End If
                    If Controls.Down(True, True, False, True, True, True) = True Then
                        If FriendListSelect = FriendListScroll + 8 Then
                            FriendListScroll += c
                        End If
                        FriendListSelect += c
                    End If
                    If Controls.Up(True, False, True, False, False, False) = True Then
                        FriendListScroll -= c
                        FriendListSelect -= c
                    End If
                    If Controls.Down(True, False, True, False, False, False) = True Then
                        FriendListScroll += c
                        FriendListSelect += c
                    End If

                    If Controls.Accept(True, False, False) = True Then
                        Dim startPos As Vector2 = GetStartPosition()
                        For i = 0 To 8
                            Dim r As New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + i * 36), 510, 32)
                            If r.Contains(MouseHandler.MousePosition) Then
                                If FriendListSelect = i + FriendListScroll Then
                                    SoundManager.PlaySound("select")
                                    PSSFriendlistDisplayUser()
                                Else
                                    FriendListSelect = i + FriendListScroll
                                End If
                                Exit For
                            End If
                        Next
                    End If

                    If FriendListScroll > FriendListSelect Then
                        FriendListScroll = FriendListSelect
                    End If
                    If FriendListScroll + 8 < FriendListSelect Then
                        FriendListScroll = FriendListSelect - 8
                    End If

                    FriendListScroll = FriendListScroll.Clamp(0, FriendList.Count - 9)
                    FriendListSelect = FriendListSelect.Clamp(0, FriendList.Count - 1)

                    If Controls.Accept(False, True, True) = True Then
                        SoundManager.PlaySound("select")
                        PSSFriendlistDisplayUser()
                    End If
                End If
            End If
        End Sub

        Private Sub InitializeFriends()
            If PublicKeys <> "" Then
                InitializedFriends = True
                FriendList.Clear()
                Dim friends() As String = Core.GameJoltSave.Friends.Split(CChar(","))

                For Each friend_user_id As String In friends
                    Dim APICall As New APICall(AddressOf GotFriendListData)

                    APICall.FetchUserdataByID(friend_user_id)
                Next
            End If
        End Sub

        Private Sub GotFriendListData(ByVal result As String)
            Dim list As List(Of API.JoltValue) = API.HandleData(result)

            For Each Item As API.JoltValue In list
                Select Case Item.Name.ToLower()
                    Case "username"
                        FriendList.Add(New Emblem(Item.Value, PublicKeys, False))

                        FriendList = (From f In FriendList Order By f.Username Ascending).ToList()

                        Exit Sub
                End Select
            Next
        End Sub

        Private Sub PSSFriendlistDisplayUser()
            UserEmblem = FriendList(FriendListSelect)
            UserSprite = UserEmblem.SpriteTexture
            UserName = UserEmblem.Username
            UserOrigin = "GJ"
            For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                If p.GameJoltId <> "" Then
                    If UserEmblem.GameJoltID = p.GameJoltId Then
                        UserOrigin &= "|Server"
                    End If
                End If
            Next
            menuIndex = MenuScreens.UserView
            UserViewPreMenu = MenuScreens.PSS
        End Sub

#End Region

#Region "Local"

        Dim LocalList As New List(Of LocalPlayer)
        Dim InitializedLocals As Boolean = False
        Dim LocalScroll As Integer = 0
        Dim LocalSelect As Integer = 0

        Class LocalPlayer

            Public NetworkID As Integer = 0
            Public Sprite As Texture2D
            Public Name As String
            Public GamejoltID As String = ""

            Public Sub New(ByVal NetworkID As Integer)
                Me.NetworkID = NetworkID
            End Sub

            Public Sub LoadOnlineSprite()
                Dim t As New Threading.Thread(AddressOf DownloadSprite)
                t.IsBackground = True
                t.Start()
            End Sub

            Private Sub DownloadSprite()
                If GamejoltID <> "" Then
                    Dim t As Texture2D = Emblem.GetOnlineSprite(GamejoltID)
                    If Not t Is Nothing Then
                        Me.Sprite = t
                    End If
                End If
            End Sub

        End Class

        Private Sub FillLocalList()
            LocalList.Clear()
            If ConnectScreen.Connected = True Then
                For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                    If p.ServersID <> Core.ServersManager.ID Then
                        Dim t As Texture2D
                        Dim tPath As String = NetworkPlayer.GetTexturePath(p.Skin)
                        If TextureManager.TextureExist(tPath) = True Then
                            t = TextureManager.GetTexture(tPath)
                        Else
                            t = TextureManager.GetTexture("Textures\NPC\0")
                        End If
                        Dim lP As New LocalPlayer(p.ServersID)

                        lP.GamejoltID = p.GameJoltId
                        lP.Name = p.Name
                        lP.Sprite = t
                        lP.LoadOnlineSprite()

                        LocalList.Add(lP)
                    End If
                Next
            End If
        End Sub

        Private Sub DrawLocalList()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16), CInt(startPos.Y + 46), 152, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 200 + 200 + 16 + 152), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, SpriteEffects.FlipVertically, 0.0F)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Ranklist", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Friendlist", New Vector2(CInt(startPos.X + 48 + 200), CInt(startPos.Y + 48)), Color.Black)
            Core.SpriteBatch.DrawString(FontManager.MainFont, "PSS Passby", New Vector2(CInt(startPos.X + 48 + 200 + 200), CInt(startPos.Y + 48)), Color.Black)


            If InitializedLocals = True Then
                If LocalList.Count > 0 Then
                    For i = 0 To 8
                        Dim index As Integer = i + LocalScroll

                        If index <= LocalList.Count - 1 Then
                            Dim lP As LocalPlayer = LocalList(index)

                            Dim eff As SpriteEffects = SpriteEffects.None
                            If index = LocalSelect Then
                                eff = SpriteEffects.FlipVertically
                            End If

                            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + i * 36), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80 + i * 36), 480, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 80 + i * 36), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                            Dim frameSize As New Size(CInt(lP.Sprite.Width / 3), CInt(lP.Sprite.Height / 4))
                            Dim frameScale As Single = 1.0F
                            If lP.Sprite.Width = lP.Sprite.Height / 2 Then
                                frameSize.Width = CInt(lP.Sprite.Width / 2)
                            ElseIf lP.Sprite.Width = lP.Sprite.Height Then
                                frameSize.Width = CInt(lP.Sprite.Width / 4)
                            Else
                                frameSize.Width = CInt(lP.Sprite.Width / 3)
                            End If
                            If frameSize.Width > 32 Then
                                frameScale = CSng(32 / frameSize.Width)
                            End If
                            If frameSize.Width <= 16 Then
                                frameScale = 2.0F
                            End If
                            Core.SpriteBatch.Draw(lP.Sprite, New Rectangle(CInt(startPos.X + 64 - (frameSize.Width * frameScale / 2)), CInt(startPos.Y + 96 - (frameSize.Height * frameScale / 2) + i * 36), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)

                            Core.SpriteBatch.DrawString(FontManager.MainFont, lP.Name, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 80 + i * 36)), Color.Black)
                        End If
                    Next
                    Canvas.DrawScrollBar(New Vector2(startPos.X + 570, startPos.Y + 88), LocalList.Count, 9, LocalScroll, New Size(4, 300), False, New Color(252, 196, 68), New Color(217, 120, 18))
                Else
                    If ConnectScreen.Connected = True Then
                        Core.SpriteBatch.DrawString(FontManager.MainFont, "No other players connected to server" & Environment.NewLine & """" & JoinServerScreen.SelectedServer.GetName() & """.", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 88)), Color.Black)
                    Else
                        Core.SpriteBatch.DrawString(FontManager.MainFont, "You are playing locally.", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 88)), Color.Black)
                    End If
                End If
            End If
        End Sub

        Private Sub UpdateLocalList()
            If InitializedLocals = False Then
                InitializedLocals = True
                FillLocalList()
            Else
                If LocalList.Count > 0 Then
                    Dim c As Integer = 1
                    If Controls.ShiftDown() = True Then
                        c = 5
                    End If

                    If Controls.Up(True, True, False, True, True, True) = True Then
                        If LocalSelect = LocalScroll Then
                            LocalScroll -= c
                        End If
                        LocalSelect -= c
                    End If
                    If Controls.Down(True, True, False, True, True, True) = True Then
                        If LocalSelect = LocalScroll + 8 Then
                            LocalScroll += c
                        End If
                        LocalSelect += c
                    End If
                    If Controls.Up(True, False, True, False, False, False) = True Then
                        LocalScroll -= c
                        LocalSelect -= c
                    End If
                    If Controls.Down(True, False, True, False, False, False) = True Then
                        LocalScroll += c
                        LocalSelect += c
                    End If

                    If Controls.Accept(True, False, False) = True Then
                        Dim startPos As Vector2 = GetStartPosition()
                        For i = 0 To 8
                            Dim r As New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + i * 36), 510, 32)
                            If r.Contains(MouseHandler.MousePosition) Then
                                If LocalSelect = i + LocalScroll Then
                                    SoundManager.PlaySound("select")
                                    PSSLocallistDisplayUser()
                                Else
                                    LocalSelect = i + LocalScroll
                                End If
                                Exit For
                            End If
                        Next
                    End If

                    If LocalScroll > LocalSelect Then
                        LocalScroll = LocalSelect
                    End If
                    If LocalScroll + 8 < LocalSelect Then
                        LocalScroll = LocalSelect - 8
                    End If

                    LocalScroll = LocalScroll.Clamp(0, LocalList.Count - 9)
                    LocalSelect = LocalSelect.Clamp(0, LocalList.Count - 1)

                    If Controls.Accept(False, True, True) = True Then
                        SoundManager.PlaySound("select")
                        PSSLocallistDisplayUser()
                    End If
                End If
            End If
        End Sub

        Private Sub PSSLocallistDisplayUser()
            If LocalList(LocalSelect).GamejoltID <> "" Then
                UserEmblem = New Emblem(LocalList(LocalSelect).GamejoltID, 0)
            Else
                UserEmblem = Nothing
            End If

            UserSprite = LocalList(LocalSelect).Sprite
            UserName = LocalList(LocalSelect).Name
            UserOrigin = "Server"
            UserNetworkID = LocalList(LocalSelect).NetworkID

            menuIndex = MenuScreens.UserView
            UserViewPreMenu = MenuScreens.PSS
        End Sub

#End Region

#End Region

#Region "UserViewScreen"

        Dim UserEmblem As Emblem = Nothing
        Dim UserSprite As Texture2D = Nothing
        Dim UserName As String = ""
        Dim UserOrigin As String = ""
        Dim UserViewPreMenu As MenuScreens = MenuScreens.Main
        Dim UserNetworkID As Integer = -1

        Private Sub DrawUserView()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 480, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 480), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 200), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Dim FrameSize As New Size(CInt(UserSprite.Width / 3), CInt(UserSprite.Height / 4))
            Dim FrameScale As Single = 1.0F
            If UserSprite.Width = UserSprite.Height / 2 Then
                FrameSize.Width = CInt(UserSprite.Width / 2)
            ElseIf UserSprite.Width = UserSprite.Height Then
                FrameSize.Width = CInt(UserSprite.Width / 4)
            Else
                FrameSize.Width = CInt(UserSprite.Width / 3)
            End If
            If FrameSize.Width > 32 Then
                FrameScale = CSng(32 / FrameSize.Width)
            End If
            If FrameSize.Width <= 16 Then
                FrameScale = 2.0F
            End If
            Core.SpriteBatch.Draw(UserSprite, New Rectangle(CInt(startPos.X + 64 - (FrameSize.Width * FrameScale / 2)), CInt(startPos.Y + 46 + 16 - (FrameSize.Height * FrameScale / 2)), CInt(FrameSize.Width * FrameScale), CInt(FrameSize.Height * FrameScale)), New Rectangle(0, FrameSize.Height * 2, FrameSize.Width, FrameSize.Height), Color.White)
            Core.SpriteBatch.DrawString(FontManager.MainFont, UserName, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 48)), Color.Black)

            If UserOrigin.Contains("GJ") = True Then
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 284), CInt(startPos.Y + 46), 16, 16), New Rectangle(80, 112, 16, 16), Color.White)
            End If
            If UserOrigin.Contains("Server") = True Then
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 308), CInt(startPos.Y + 46), 16, 16), New Rectangle(112, 112, 16, 16), Color.White)
            End If

            Dim sameServer As Boolean = False
            Dim sameConnection As Boolean = False

            If Core.Player.IsGameJoltSave = True Then
                If Not UserEmblem Is Nothing Then
                    sameConnection = True
                End If
            Else
                If UserEmblem Is Nothing Then
                    sameConnection = True
                End If
            End If

            If ConnectScreen.Connected = True Then
                For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                    If p.Name = UserName Then
                        sameServer = True
                    End If
                Next
            End If

            For x = 0 To 4
                Dim c As Color = Color.White
                Dim t As String = ""
                Dim texV As Vector2 = New Vector2
                Select Case x
                    Case 0
                        t = "Battle"
                        If sameServer = False Then
                            c = Color.Gray
                        End If
                        texV = New Vector2(32, 96)
                    Case 1
                        t = "Trade"
                        If sameServer = False Or sameConnection = False Then
                            c = Color.Gray
                        End If
                        texV = New Vector2(0, 96)
                    Case 2
                        t = "PM"
                        If sameServer = False Then
                            c = Color.Gray
                        End If
                    Case 3
                        t = "Friend"
                        If UserEmblem Is Nothing Then
                            c = Color.Gray
                        End If
                    Case 4
                        t = "Favorite"
                        If UserEmblem Is Nothing Then
                            c = Color.Gray
                        End If
                End Select
                Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 96) + 76), CInt(startPos.Y + 100), 64, 64), New Rectangle(0, 0, 32, 32), c)
                If texV <> Vector2.Zero Then
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 96) + 76), CInt(startPos.Y + 100), 64, 64), New Rectangle(CInt(texV.X), CInt(texV.Y), 32, 32), c)
                End If
                If Cursors(1) = x Then
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 96) + 76), CInt(startPos.Y + 100), 64, 64), New Rectangle(0, 32, 32, 32), Color.White)
                End If
                Core.SpriteBatch.DrawString(FontManager.MainFont, t, New Vector2(CInt(startPos.X + (x * 96) + 76) + 32 - FontManager.MainFont.MeasureString(t).X / 2.0F, CInt(startPos.Y + 170)), Color.Black)
            Next

            If Not UserEmblem Is Nothing Then
                UserEmblem.Draw(New Vector2(startPos.X + 40, startPos.Y + 240), 4)
            End If
        End Sub

        Private Sub UpdateUserView()
            If Controls.Left(True, True) = True Then
                Cursors(1) -= 1
            End If
            If Controls.Right(True, True) = True Then
                Cursors(1) += 1
            End If

            Cursors(1) = Cursors(1).Clamp(0, 4)

            Dim MouseAccept As Boolean = False
            If Controls.Accept(True, False, False) = True Then
                Dim pressedIndex As Integer = -1
                Dim startPos As Vector2 = GetStartPosition()
                For x = 0 To 4
                    Dim r As New Rectangle(CInt(startPos.X + (x * 96) + 76), CInt(startPos.Y + 100), 64, 64)
                    If r.Contains(MouseHandler.MousePosition) = True Then
                        pressedIndex = x
                    End If
                Next
                If pressedIndex > -1 Then
                    If 4 >= pressedIndex Then
                        If Cursors(1) = pressedIndex Then
                            SoundManager.PlaySound("select")
                            MouseAccept = True
                        Else
                            Cursors(1) = pressedIndex
                        End If
                    End If
                End If
            End If

            If Controls.Accept(False, True, True) = True OrElse MouseAccept = True Then
                Dim sameServer As Boolean = False
                Dim sameConnection As Boolean = False

                If Core.Player.IsGameJoltSave = True Then
                    If Not UserEmblem Is Nothing Then
                        sameConnection = True
                    End If
                Else
                    If UserEmblem Is Nothing Then
                        sameConnection = True
                    End If
                End If

                If ConnectScreen.Connected = True Then
                    For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                        If p.Name = UserName Then
                            sameServer = True
                        End If
                    Next
                End If

                Select Case Cursors(1)
                    Case 0
                        If sameServer = True Then
                            SoundManager.PlaySound("select")
                            Core.SetScreen(New PVPLobbyScreen(Core.CurrentScreen, UserNetworkID, True))
                        End If
                    Case 1
                        If sameServer = True And sameConnection = True Then
                            SoundManager.PlaySound("select")
                            Core.SetScreen(New DirectTradeScreen(Core.CurrentScreen, UserNetworkID, True))
                        End If
                    Case 2
                        If sameServer = True Then
                            Dim chat As New ChatScreen(CurrentScreen)
                            chat.EnterPMChat(UserName)
                            Core.SetScreen(chat)

                        End If
                    Case 3

                    Case 4

                End Select
            End If

            If Controls.Dismiss(True, True, True) = True Then
                SoundManager.PlaySound("select")
                menuIndex = UserViewPreMenu
            End If
        End Sub

#End Region

#Region "Phone"

        Public Shared Call_Flag As String = ""

        Private PhoneContacts As New List(Of Contact)
        Private InitializedPhone As Boolean = False
        Private PhoneScroll As Integer = 0
        Private PhoneSelect As Integer = 0

        Private Sub DrawPhone()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 192, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 192), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Phone", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)

            If PhoneContacts.Count > 0 Then
                For i = 0 To 8
                    Dim index As Integer = i + PhoneScroll

                    If index <= PhoneContacts.Count - 1 Then
                        Dim C As Contact = PhoneContacts(index)

                        Dim eff As SpriteEffects = SpriteEffects.None
                        If index = PhoneSelect Then
                            eff = SpriteEffects.FlipVertically
                        End If

                        Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + i * 36), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                        Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 80 + i * 36), 584, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                        Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 584), CInt(startPos.Y + 80 + i * 36), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                        Dim _spriteTexture As Texture2D = TextureManager.GetTexture("Textures\NPC\" & C.Texture)

                        Dim frameSize As New Size(CInt(_spriteTexture.Width / 3), CInt(_spriteTexture.Height / 4))
                        Dim frameScale As Single = 1.0F
                        If _spriteTexture.Width = _spriteTexture.Height / 2 Then
                            frameSize.Width = CInt(_spriteTexture.Width / 2)
                        ElseIf _spriteTexture.Width = _spriteTexture.Height Then
                            frameSize.Width = CInt(_spriteTexture.Width / 4)
                        Else
                            frameSize.Width = CInt(_spriteTexture.Width / 3)
                        End If
                        If frameSize.Width > 32 Then
                            frameScale = CSng(32 / frameSize.Width)
                        End If
                        If frameSize.Width <= 16 Then
                            frameScale = 2.0F
                        End If
                        Core.SpriteBatch.Draw(_spriteTexture, New Rectangle(CInt(startPos.X + 48), CInt(startPos.Y + 96 - (frameSize.Height * frameScale / 2) + i * 36), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)
                        Core.SpriteBatch.DrawString(FontManager.MainFont, C.Name, New Vector2(CInt(startPos.X + 88), CInt(startPos.Y + 84 + i * 36)), Color.Black)
                        Core.SpriteBatch.DrawString(FontManager.MainFont, "Location: " & C.Location, New Vector2(CInt(startPos.X + 320), CInt(startPos.Y + 84 + i * 36)), Color.Black)
                    End If
                Next
                Canvas.DrawScrollBar(New Vector2(startPos.X + width - 32, startPos.Y + 88), PhoneContacts.Count, 9, PhoneScroll, New Size(4, 300), False, New Color(252, 196, 68), New Color(217, 120, 18))
            End If
        End Sub

        Private Sub UpdatePhone()
            If InitializedPhone = False Then
                InitializePhone()
            Else
                If PhoneContacts.Count > 0 Then

                    Dim c As Integer = 1
                    If Controls.ShiftDown() = True Then
                        c = 5
                    End If

                    If Controls.Up(True, True, False, True, True, True) = True Then
                        If PhoneSelect = PhoneScroll Then
                            PhoneScroll -= c
                        End If
                        PhoneSelect -= c
                    End If
                    If Controls.Down(True, True, False, True, True, True) = True Then
                        If PhoneSelect = PhoneScroll + 8 Then
                            PhoneScroll += c
                        End If
                        PhoneSelect += c
                    End If
                    If Controls.Up(True, False, True, False, False, False) = True Then
                        PhoneScroll -= c
                        PhoneSelect -= c
                    End If
                    If Controls.Down(True, False, True, False, False, False) = True Then
                        PhoneScroll += c
                        PhoneSelect += c
                    End If

                    If Controls.Accept(True, False, False) = True Then
                        Dim startPos As Vector2 = GetStartPosition()
                        For i = 0 To 8
                            Dim r As New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 80 + i * 36), 510, 32)
                            If r.Contains(MouseHandler.MousePosition) Then
                                If PhoneSelect = i + PhoneScroll Then
                                    SoundManager.PlaySound("select")
                                    SelectPhoneContact()
                                Else
                                    PhoneSelect = i + PhoneScroll
                                End If
                                Exit For
                            End If
                        Next
                    End If

                    If PhoneScroll > PhoneSelect Then
                        PhoneScroll = PhoneSelect
                    End If
                    If PhoneScroll + 8 < PhoneSelect Then
                        PhoneScroll = PhoneSelect - 8
                    End If

                    PhoneScroll = PhoneScroll.Clamp(0, PhoneContacts.Count - 9)
                    PhoneSelect = PhoneSelect.Clamp(0, PhoneContacts.Count - 1)

                    If Controls.Accept(False, True, True) = True Then
                        SoundManager.PlaySound("select")
                        SelectPhoneContact()
                    End If
                End If
            End If
            If Controls.Dismiss(True, True, True) = True Then
                SoundManager.PlaySound("select")
                menuIndex = MenuScreens.Main
            End If
        End Sub

        Private Sub SelectPhoneContact()
            Dim chosenID As String = PhoneContacts(PhoneSelect).ID

            Call_Flag = "calling"

            Player.Temp.PokegearPage = Me.menuIndex
            Core.SetScreen(Me.PreScreen)
            CType(Core.CurrentScreen, OverworldScreen).ActionScript.StartScript("phone\" & chosenID, 0,,, "PhoneCall")
        End Sub

        Private Sub InitializePhone()
            InitializedPhone = True
            If System.IO.File.Exists(GameModeManager.GetContentFilePath("Data\contacts.dat")) Then
                Dim reg() As String = Core.Player.RegisterData.Split(CChar(","))

                Dim contactData() As String = System.IO.File.ReadAllLines(GameModeManager.GetContentFilePath("Data\contacts.dat"))

                For Each r As String In reg
                    If r.StartsWith("phone_contact_") = True Then
                        Dim newID As String = r.Remove(0, "phone_contact_".Length)

                        For Each line As String In contactData
                            If line.StartsWith(newID & "|") = True Then
                                Dim NCD() As String = line.Split(CChar("|"))

                                Dim newContact As New Contact With {.ID = NCD(0), .Name = NCD(1), .Texture = NCD(2), .Location = NCD(3), .CanRandomCall = CBool(NCD(4))}

                                PhoneContacts.Add(newContact)
                            End If
                        Next
                    End If
                Next
            End If

            PhoneContacts = (From c In PhoneContacts Order By c.ID Ascending).ToList()
        End Sub

        Private Structure Contact
            Dim Name As String
            Dim ID As String
            Dim Texture As String
            Dim Location As String
            Dim CanRandomCall As Boolean
        End Structure

        Public Shared Sub CallID(ByVal ID As String, ByVal checkRegistered As Boolean, ByVal checkLocation As Boolean)
            Dim reg() As String = Core.Player.RegisterData.Split(CChar(","))

            Dim file As String = GameModeManager.GetContentFilePath("Data\contacts.dat")

            Security.FileValidation.CheckFileValid(file, False, "PokegearScreen.vb")
            Dim contactData() As String = System.IO.File.ReadAllLines(file)

            Dim tempContacts As New List(Of Contact)

            For Each r As String In reg
                If r.StartsWith("phone_contact_") = True Then
                    Dim newID As String = r.Remove(0, "phone_contact_".Length)

                    If newID = ID Then
                        For Each line As String In contactData
                            If line.StartsWith(newID & "|") = True Or checkRegistered = False Then
                                Dim NCD() As String = line.Split(CChar("|"))

                                Dim newContact As New Contact With {.ID = NCD(0), .Name = NCD(1), .Texture = NCD(2), .Location = NCD(3), .CanRandomCall = CBool(NCD(4))}

                                tempContacts.Add(newContact)
                            End If
                        Next
                    End If
                End If
            Next

            Dim countPossibleContacts As Integer = 0
            For Each c As Contact In tempContacts
                If c.Location.ToLower() <> Level.MapName.ToLower() Or checkLocation = False Then
                    countPossibleContacts += 1
                End If
            Next

            If countPossibleContacts > 0 Then
                Dim chosenID As String = "-1"
                While chosenID = "-1"
                    Dim nC As Contact = tempContacts(Core.Random.Next(0, tempContacts.Count))

                    If nC.Location.ToLower() <> Level.MapName.ToLower() Or checkLocation = False Then
                        chosenID = nC.ID
                    End If
                End While

                Call_Flag = "receiving"

                CType(Core.CurrentScreen, OverworldScreen).ActionScript.StartScript("phone\" & chosenID, 0,,, "PhoneReceiving")
            End If
        End Sub

        Public Shared Sub RandomCall()
            If Core.CurrentScreen.Identification <> Identifications.OverworldScreen Then
                Exit Sub
            End If
            If System.IO.File.Exists(GameModeManager.GetContentFilePath("Data\contacts.dat")) = False Then
                Exit Sub
            End If

            Dim reg() As String = Core.Player.RegisterData.Split(CChar(","))

            Dim file As String = GameModeManager.GetContentFilePath("Data\contacts.dat")

            Security.FileValidation.CheckFileValid(file, False, "PokegearScreen.vb")
            Dim contactData() As String = System.IO.File.ReadAllLines(file)

            Dim tempContacts As New List(Of Contact)

            For Each r As String In reg
                If r.StartsWith("phone_contact_") = True Then
                    Dim newID As String = r.Remove(0, "phone_contact_".Length)

                    For Each line As String In contactData
                        If line.StartsWith(newID & "|") = True Then
                            Dim NCD() As String = line.Split(CChar("|"))

                            Dim newContact As New Contact With {.ID = NCD(0), .Name = NCD(1), .Texture = NCD(2), .Location = NCD(3), .CanRandomCall = CBool(NCD(4))}

                            tempContacts.Add(newContact)
                        End If
                    Next
                End If
            Next

            Dim countPossibleContacts As Integer = 0
            For Each c As Contact In tempContacts
                If c.CanRandomCall = True And c.Location.ToLower() <> Level.MapName.ToLower() Then
                    countPossibleContacts += 1
                End If
            Next

            If countPossibleContacts > 0 Then
                Dim chosenID As String = "-1"
                While chosenID = "-1"
                    Dim nC As Contact = tempContacts(Core.Random.Next(0, tempContacts.Count))

                    If nC.CanRandomCall = True And nC.Location.ToLower() <> Level.MapName.ToLower() Then
                        chosenID = nC.ID
                    End If
                End While

                Call_Flag = "receiving"

                CType(Core.CurrentScreen, OverworldScreen).ActionScript.StartScript("phone\" & chosenID, 0,,, "PhoneReceiving")
            End If
        End Sub

#End Region

#Region "Frontier"

        Class FrontierSymbol

            Public Name As String
            Public Texture As Texture2D
            Public Description As String

        End Class

        Private FrontierList As New List(Of FrontierSymbol)
        Private InitializedFrontier As Boolean = False

        Private Sub DrawFrontier()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 224, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 224), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Frontier Emblems", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Current BP: " & Core.Player.BP.ToString, New Vector2(CInt(startPos.X + 320), CInt(startPos.Y + 48)), Color.Black)

            If FrontierList.Count > 0 Then
                For x = 0 To FrontierList.Count - 1
                    Dim c As Color = Color.White
                    Dim t As String = FrontierList(x).Name

                    Core.SpriteBatch.Draw(FrontierList(x).Texture, New Rectangle(CInt(startPos.X + (x * 96) + 83), CInt(startPos.Y + 107), 50, 50), c)
                    If Cursors(2) = x Then
                        Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + (x * 96) + 76), CInt(startPos.Y + 100), 64, 64), New Rectangle(0, 32, 32, 32), Color.White)
                        Core.SpriteBatch.DrawString(FontManager.MainFont, t, New Vector2(CInt(startPos.X + (x * 96) + 76) + 32 - FontManager.MainFont.MeasureString(t).X / 2.0F, CInt(startPos.Y + 170)), Color.Black)

                        Canvas.DrawGradient(New Rectangle(CInt(startPos.X + 48), CInt(startPos.Y + 220), width - 100, heigth - 260), Color.White, Color.Gray, False, -1)

                        Core.SpriteBatch.DrawString(FontManager.MainFont, FrontierList(x).Description, New Vector2(CInt(startPos.X + 55), CInt(startPos.Y + 225)), Color.Black)
                    End If
                Next
            Else
                Core.SpriteBatch.DrawString(FontManager.MainFont, "You don't own any Frontier Emblems yet.", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 88)), Color.Black)
                Canvas.DrawGradient(New Rectangle(CInt(startPos.X + 48), CInt(startPos.Y + 220), width - 100, heigth - 260), Color.White, Color.Gray, False, -1)
            End If
        End Sub

        Private Sub UpdateFrontier()
            If InitializedFrontier = False Then
                InitializeFrontier()
            Else
                If Controls.Right(True, True, True, True, True, True) = True Then
                    Cursors(2) += 1
                End If
                If Controls.Left(True, True, True, True, True, True) = True Then
                    Cursors(2) -= 1
                End If

                Cursors(2) = Cursors(2).Clamp(0, FrontierList.Count - 1)
            End If

            If Controls.Dismiss(True, True, True) = True Then
                Me.menuIndex = MenuScreens.Main
            End If
        End Sub

        Private Sub InitializeFrontier()
            FrontierList.Clear()

            Me.InitializedFrontier = True

            Dim file As String = GameModeManager.GetContentFilePath("Data\badges.dat")
            Security.FileValidation.CheckFileValid(file, False, "Badge.vb")
            Dim data() As String = System.IO.File.ReadAllLines(file)
            For Each line As String In data
                If line.Contains("|") = True AndAlso StringHelper.IsNumeric(line.GetSplit(0, "|")) = False Then

                    Dim hasGold As Boolean = False
                    Dim hasSilver As Boolean = False
                    If line.GetSplit(0, "|").EndsWith("_gold") Then
                        If ActionScript.IsRegistered(line.GetSplit(0, "|")) = True Then
                            hasGold = True
                        End If
                    End If
                    If line.GetSplit(0, "|").EndsWith("_silver") Then
                        If ActionScript.IsRegistered(line.GetSplit(0, "|")) = True Then
                            If ActionScript.IsRegistered(CStr(line.GetSplit(0, "|").Remove(line.GetSplit(0, "|").Length - "_silver".Length, "_silver".Length) & "_gold")) = False Then
                                hasSilver = True
                            End If
                        End If
                    End If
                    If hasSilver = True OrElse hasGold = True Then
                        Dim emblemName As String = ""
                        Dim emblemDescription As String = ""
                        Dim emblemTexture As Texture2D = Nothing
                        Dim lineData As String() = line.Split("|")
                        For i = 1 To lineData.Count - 1
                            If lineData(i).ToLower.StartsWith("name=") Then
                                emblemName = Localization.GetString(line.GetSplit(0, "|") & "_name", lineData(i).Remove(0, "name=".Length))
                            End If
                            If lineData(i).ToLower.StartsWith("description=") Then
                                emblemDescription = Localization.GetString(line.GetSplit(0, "|") & "_description", lineData(i).Remove(0, "description=".Length))
                            End If
                            If lineData(i).ToLower.StartsWith("texture=") Then
                                Dim texturedata As String() = lineData(i).Remove(0, "texture=".Length).Split(",")
                                emblemTexture = TextureManager.GetTexture(texturedata(0), New Rectangle(CInt(texturedata(1)), CInt(texturedata(2)), CInt(texturedata(3)), CInt(texturedata(4))), "")
                            End If
                        Next
                        If emblemName = "" Then
                            If Localization.TokenExists(line.GetSplit(0, "|") & "_name") = True Then
                                emblemName = Localization.GetString(line.GetSplit(0, "|") & "_name")
                            End If
                        End If
                        If emblemDescription = "" Then
                            If Localization.TokenExists(line.GetSplit(0, "|") & "_description") = True Then
                                emblemDescription = Localization.GetString(line.GetSplit(0, "|") & "_description")
                            End If
                        End If
                        If emblemTexture IsNot Nothing Then
                            Me.FrontierList.Add(New FrontierSymbol() With {.Name = emblemName.Replace("~", Environment.NewLine), .Description = emblemDescription.Replace("~", Environment.NewLine), .Texture = emblemTexture})
                        End If

                    End If
                End If
            Next

        End Sub

#End Region

#Region "Minimap"

        Private MiniMap As Minimap
        Private InitializedMinimap As Boolean = False

        Private Sub DrawMinimap()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 365), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 365 + 16), CInt(startPos.Y + 46), 128, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 365 + 16 + 128), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Minimap", New Vector2(CInt(startPos.X + 372), CInt(startPos.Y + 48)), Color.Black)

            If InitializedMinimap = True Then
                Canvas.DrawBorder(1, New Rectangle(CInt(startPos.X + 49 - 32), CInt(startPos.Y + 79 - 32), 21 * 16 + 2, 21 * 16 + 2), Color.Black)
                MiniMap.Draw(New Vector2(-(startPos.X + 48), -(startPos.Y + 80)))
            End If
        End Sub

        Private Sub UpdateMinimap()
            If InitializedMinimap = False Then
                InitializedMinimap = True
                MiniMap = New Minimap()
                MiniMap.Initialize()
            End If

            If Controls.Dismiss(True, True, True) = True Then
                Me.menuIndex = MenuScreens.Main
            End If
        End Sub

#End Region

#Region "Radio"

        Class RadioStation

            Enum ExpansionCards
                RADIO
                EXPNS
            End Enum

            Public ChannelMin As Decimal
            Public ChannelMax As Decimal
            Public OverwriteMin As Decimal
            Public OverwriteMax As Decimal
            Public Name As String = ""
            Public Region As String = ""
            Public DayTimes As New List(Of World.DayTimes)
            Public Expansions As New List(Of String)
            Public Music As String = ""
            Public Content As String = ""
            Public CanBeOverwritten As Boolean = True
            Public Activation As String = ""
            Public ActivationRegister As String = ""

            Public Sub New(ByVal input As String)
                Dim data() As String = input.Split(CChar("|"))

                If data(0).Contains("-") = True Then
                    Dim channelData() As String = data(0).Split(CChar("-"))
                    Me.ChannelMin = CDec(channelData(0).Replace(".", GameController.DecSeparator))
                    Me.ChannelMax = CDec(channelData(1).Replace(".", GameController.DecSeparator))
                Else
                    Me.ChannelMin = CDec(data(0).Replace(".", GameController.DecSeparator))
                    Me.ChannelMax = Me.ChannelMin
                End If

                If data(1).Contains("-") = True Then
                    Dim channelData() As String = data(1).Split(CChar("-"))
                    Me.OverwriteMin = CDec(channelData(0).Replace(".", GameController.DecSeparator))
                    Me.OverwriteMax = CDec(channelData(1).Replace(".", GameController.DecSeparator))
                Else
                    Me.OverwriteMin = CDec(data(1).Replace(".", GameController.DecSeparator))
                    Me.OverwriteMax = Me.OverwriteMin
                End If

                Me.Name = data(2)
                Me.Region = data(3)

                Dim lDayTimes() As String = data(4).Split(CChar(","))
                For Each daytime As String In lDayTimes
                    If StringHelper.IsNumeric(daytime) = True Then
                        DayTimes.Add(CType(CInt(daytime), World.DayTimes))
                    End If
                Next

                If data(5) <> "" Then
                    For Each exp As String In data(5).Split(CChar(","))
                        Me.Expansions.Add(exp)
                    Next
                End If

                Me.Music = data(6)
                Me.Content = data(7)

                Me.CanBeOverwritten = CBool(data(8))

                Me.Activation = data(9)
                Me.ActivationRegister = data(10)
            End Sub

            Public Function CanListen() As Boolean
                ' Need to check: Daytime, Region, Expansion Card, Activation

                If Me.DayTimes.Contains(World.GetTime) = False Then
                    Return False
                End If

                Dim regions() As String = Screen.Level.CurrentRegion.ToLower().Split(CChar(","))
                If regions.Contains(Me.Region.ToLower()) = False Then
                    Return False
                End If

                For Each exp As String In Me.Expansions
                    If exp.ToLower <> "radio" Then
                        If ActionScript.IsRegistered("pokegear_card_radio" & exp) = False Then
                            Return False
                        End If
                    End If
                Next

                Select Case Activation
                    Case "1" ' Needs register in the level channels (only works when minChannel = maxChannel)
                        If Screen.Level.AllowedRadioChannels.Contains(Me.ChannelMin) = False Then
                            Return False
                        End If
                    Case "0"
                        ' Channel is always available.
                End Select

                If Me.ActivationRegister <> "0" Then
                    If ActionScript.IsRegistered(Me.ActivationRegister) = False Then
                        Return False
                    End If
                End If

                Return True
            End Function

            Public Function OverwriteChannels(ByVal ChannelList As List(Of RadioStation)) As List(Of RadioStation)
                If Me.CanListen() = False Then
                    Return ChannelList
                End If

                Dim newList As New List(Of RadioStation)
                For Each c As RadioStation In ChannelList
                    If c.CanBeOverwritten = True Then
                        If c.ChannelMin < Me.OverwriteMin Or c.ChannelMax > Me.OverwriteMax Then
                            newList.Add(c)
                        End If
                    End If
                Next

                newList.Add(Me)

                Return newList
            End Function

            Public Function IsInterfering(ByVal frequenz As Decimal) As Boolean
                If frequenz >= Me.ChannelMin And frequenz <= Me.ChannelMax Then
                    Return True
                End If
                Return False
            End Function

            Public Function GenerateText() As List(Of String)
                Dim output As String = "...~...~...~...~...~..."

                Select Case Me.Content.ToLower()
                    Case "[pokedexentry]"
                        Dim triedIDs As New List(Of Integer)
                        Dim triedADs As New List(Of String)
                        Dim chosenID As Integer = -1
                        Dim chosenAD As String = ""

                        While chosenID = -1 And triedIDs.Count < Pokedex.PokemonMaxCount
                            Dim ID As Integer = Core.Random.Next(1, Pokedex.PokemonMaxCount + 1)
                            If triedIDs.Contains(ID) = False Then
                                If PokemonForms.GetDataFileForms(ID).Count > 0 Then
                                    Dim FormList As List(Of String) = PokemonForms.GetDataFileForms(ID)
                                    Dim FormIndex As Integer = Core.Random.Next(0, FormList.Count)
                                    While FormList.Count > 0
                                        If Pokedex.GetEntryType(Core.Player.PokedexData, FormList(FormIndex)) < 2 Then
                                            FormList.RemoveAt(FormIndex)
                                            FormIndex = Core.Random.Next(0, FormList.Count)
                                        Else
                                            If FormList(FormIndex).Contains("_") Then
                                                chosenID = CInt(FormList(FormIndex).GetSplit(0, "_"))
                                                chosenAD = PokemonForms.GetAdditionalValueFromDataFile(FormList(FormIndex))
                                                Exit While
                                            Else
                                                chosenID = CInt(FormList(FormIndex))
                                                Exit While
                                            End If
                                        End If

                                    End While
                                    If chosenID = -1 Then
                                        triedIDs.Add(ID)
                                    End If
                                Else
                                    If Pokedex.GetEntryType(Core.Player.PokedexData, ID.ToString) < 2 Then
                                        triedIDs.Add(ID)
                                    Else
                                        chosenID = ID
                                    End If
                                End If
                            End If
                        End While

                        If chosenID > -1 Then

                            Dim p As Pokemon = Pokemon.GetPokemonByID(chosenID, chosenAD)

                            Dim DexEntryText As String = p.PokedexEntry.Text
                            Dim DexEntrySpecies As String = p.PokedexEntry.Species

                            Dim FormName As String = PokemonForms.GetFormName(p)
                            If FormName = "" Then
                                FormName = p.Name
                            End If
                            If Localization.TokenExists("pokemon_desc_" & FormName) = True Then
                                DexEntryText = Localization.GetString("pokemon_desc_" & FormName, p.PokedexEntry.Text)
                            End If
                            If Localization.TokenExists("pokemon_species_" & FormName) = True Then
                                DexEntrySpecies = Localization.GetString("pokemon_species_" & FormName, p.PokedexEntry.Species)
                            End If

                            output = "Welcome to the Pokédex Show! Today, we are going to look at the entry of " & p.GetName(True) & "! Its entry reads:~""" & DexEntryText & """~Wow, that is interesting! Also, " & p.GetName(True) & " is " & p.PokedexEntry.Height & "m high and weighs " & p.PokedexEntry.Weight & "kg.~Isn't that amazing?~" & p.GetName(True) & " is part of the " & DexEntrySpecies & " species.~That's all the information we have. Tune in next time!"
                        End If
                    Case "[randompokemon]"
                        Dim levels() As String = {"route29.dat", "route30.dat", "route31.dat", "route32.dat", "route33.dat", "route36.dat", "route37.dat", "route38.dat", "route39.dat", "routes\route34.dat", "routes\route35.dat", "routes\route42.dat", "routes\route43.dat", "routes\route44.dat", "routes\route45.dat", "routes\route46.dat"}
                        Dim cLevel As String = levels(Core.Random.Next(0, levels.Count))
                        Dim p As Pokemon = Spawner.GetPokemon(cLevel, 0, False)

                        Dim levelName As String = cLevel
                        If levelName.Contains("\") = True Then
                            levelName = levelName.Remove(0, levelName.LastIndexOf("\") + 1)
                        End If
                        levelName = levelName.Remove(levelName.Length - 4, 4)
                        levelName = levelName(0).ToString().ToUpper() & levelName.Remove(0, 1)
                        levelName = levelName.Substring(0, 5) & " " & levelName.Remove(0, 5)

                        If Not p Is Nothing Then
                            output = "Professor Oak's Pokémon Talk! With Mary!~~Professor Oak: " & p.GetName(True) & " has been spotted on " & levelName & ".~Mary: " & p.GetName(True) & "! How smart! How inspiring!"
                        End If
                    Case "[unown]"
                        Dim words() As String = {"doom", "dark", "help", "join us", "stay", "lost", "vanish", "always there", "no eyes"}
                        output = ""
                        For x = 0 To 50
                            If output <> "" Then
                                output &= "~"
                            End If
                            If Core.Random.Next(0, 3) = 0 Then
                                output &= words(Core.Random.Next(0, words.Count))
                            Else
                                output &= "... ... ..."
                            End If
                        Next
                    Case "[luckychannel]"
                        output = "We are not broadcasting at the moment.~We will ensure that we can provide our service to you again as soon as possible."
                    Case "[placesandpeople]"
                        Dim phrases() As String = {" is actually great.", " is always happy.", " is cute.", " is definitely odd!", " is inspiring!", " is just my type.", " is just so-so.", " is kind of weird.", " is precious.", " is quite noisy.", " is right for me?", " is so cool, no?", " is sort of OK.", " is sort of lazy.", " is somewhat bold.", " is too picky!"}
                        Dim people() As String = {"Youngster Joey", "Youngster Mike", "Bug Catcher Don", "Schoolboy Danny", "Cooltrainer Quinn", "Bug Catcher Rob", "Bug Catcher Doug", "Bug Catcher Ed", "Youngster Warren", "Youngster Jimmy", "Firebreather Otis", "Firebreather Burt", "Pickniker Hope", "Bird Keeper Hank", "Picnicker Sharon", "Pokéfan Rex", "Pokéfan Allan", "Biker Dwayne", "Biker Harris", "Biker Zeke", "Super Nerd Sam", "Super Nerd Tom", "Picnicker Edna", "Camper Sid", "Camper Dean", "Hiker Tim", "Picnicker Heidi", "Hiker Sidney", "Pokéfan Robert", "Hiker Jim", "Psychic Fidel", "Youngster Jason", "Youngster Owen", "Psychic Herman", "Fisher Kile", "Fisher Martin", "Fisher Stephen", "Fisher Barney"}

                        output = "This is Places and People~with Lily.~Let's have a look at some~interesting people today."

                        For x = 0 To 10
                            Dim phrase As String = phrases(Core.Random.Next(0, phrases.Count))
                            Dim person As String = people(Core.Random.Next(0, people.Count))
                            output &= "~" & person & phrase
                        Next

                        output &= "This is it for now.~Tune in next time."
                    Case Else
                        output = Content
                End Select

                Dim l As New List(Of String)
                l = output.Split(CChar("~")).ToList()

                Dim outputList As New List(Of String)

                For Each e As String In l
                    outputList.AddRange(e.CropStringToWidth(FontManager.InGameFont, 480).SplitAtNewline())
                Next

                outputList.Insert(0, "")

                Return outputList
            End Function

        End Class

        Private RadioStations As New List(Of RadioStation)
        Private InitializedRadio As Boolean = False
        Private RadioCursor As Decimal = 0D
        Private CurrentSong As String = ""
        Private CurrentStation As RadioStation = Nothing

        Private BroadCastLines As New List(Of String)
        Private LineDelay As Single = 0.0F

        Private Sub DrawRadio()
            Dim startPos As Vector2 = GetStartPosition()
            Core.SpriteBatch.DrawString(FontManager.MainFont, "Tuning: " & Me.RadioCursor.ToString(), New Vector2(startPos.X + 100, startPos.Y + 50), Color.Black)

            Canvas.DrawRectangle(New Rectangle(CInt(startPos.X + 88), CInt(startPos.Y + 110), 420, 5), Color.Black)

            For i = 0 To 21
                Dim tP As Integer = CInt(i * 20)
                Canvas.DrawRectangle(New Rectangle(CInt(startPos.X + 88 + tP), CInt(startPos.Y + 108), 2, 9), Color.Black)
            Next

            Dim cursorPosition As Integer = CInt(RadioCursor * 20)
            Canvas.DrawRectangle(New Rectangle(CInt(startPos.X + 86 + cursorPosition), CInt(startPos.Y + 105), 8, 15), Color.White)

            Dim text1 As String = "...No channels found..."
            If Not CurrentStation Is Nothing Then
                text1 = "You are listening to:" & Environment.NewLine & CurrentStation.Name
            End If
            Core.SpriteBatch.DrawString(FontManager.MainFont, text1, New Vector2(startPos.X + 150 - CInt(FontManager.MainFont.MeasureString(text1).X / 2), startPos.Y + 152), Color.Black)

            Dim text2 As String = ""
            If Screen.Level.IsRadioOn = True Then
                text2 = "Background station:" & Environment.NewLine & Screen.Level.SelectedRadioStation.Name & "." & Environment.NewLine & "Press Accept to remove."
            Else
                If Not CurrentStation Is Nothing Then
                    text2 = "Press Accept to listen" & Environment.NewLine & "to this station in" & Environment.NewLine & "the background."
                End If
            End If
            If text2 <> "" Then
                Core.SpriteBatch.DrawString(FontManager.MainFont, text2, New Vector2(startPos.X + 480 - CInt(FontManager.MainFont.MeasureString(text2).X / 2), startPos.Y + 152), Color.Black)
            End If

            Dim CanvasTexture As Texture2D
            CanvasTexture = TextureManager.GetTexture("GUI\Menus\Menu", New Rectangle(0, 0, 48, 48), "")

            Canvas.DrawImageBorder(CanvasTexture, 2, New Rectangle(CInt(startPos.X) + 44, CInt(startPos.Y) + 260, 96 * 6, 96))

            If BroadCastLines.Count > 0 Then
                Core.SpriteBatch.DrawString(FontManager.InGameFont, BroadCastLines(0), New Vector2(CInt(startPos.X) + 44 + CInt(96 * 6 / 2) - CInt(FontManager.InGameFont.MeasureString(BroadCastLines(0)).X / 2), CInt(startPos.Y) + 286), Color.Black)
                If BroadCastLines.Count > 1 Then

                    Core.SpriteBatch.DrawString(FontManager.InGameFont, BroadCastLines(1), New Vector2(CInt(startPos.X) + 44 + CInt(96 * 6 / 2) - CInt(FontManager.InGameFont.MeasureString(BroadCastLines(1)).X / 2), CInt(startPos.Y) + 320), Color.Black)
                End If
            End If
        End Sub

        Private Sub UpdateRadio()
            If InitializedRadio = False Then
                InitializeRadio()
            Else
                Dim iniCursor As Decimal = RadioCursor
                If Controls.Right(True, True) = True Then
                    RadioCursor += 0.5D
                    If Controls.ShiftDown() = True Then
                        RadioCursor += 1.5D
                    End If
                End If
                If Controls.Left(True, True) = True Then
                    RadioCursor -= 0.5D
                    If Controls.ShiftDown() = True Then
                        RadioCursor -= 1.5D
                    End If
                End If

                RadioCursor = RadioCursor.Clamp(0D, 21D)

                If iniCursor <> RadioCursor Then
                    BroadCastLines.Clear()
                    CheckForStation()
                End If
                If Controls.Accept(True, True, True) = True Then
                    If CurrentSong = "" Then
                        Screen.Level.IsRadioOn = False
                        Screen.Level.SelectedRadioStation = Nothing
                    Else
                        If Screen.Level.IsRadioOn = True AndAlso GetSelectedStation().IsInterfering(Screen.Level.SelectedRadioStation.ChannelMin) = True Then
                            Screen.Level.IsRadioOn = False
                            Screen.Level.SelectedRadioStation = Nothing
                        Else
                            Screen.Level.IsRadioOn = Not Screen.Level.IsRadioOn
                            Screen.Level.SelectedRadioStation = CurrentStation
                            Player.Temp.RadioStation = Me.RadioCursor
                        End If
                    End If
                End If

                If Not CurrentStation Is Nothing Then
                    If BroadCastLines.Count = 0 Then
                        BroadCastLines = CurrentStation.GenerateText()
                        LineDelay = 13.0F
                    Else
                        If LineDelay > 0.0F Then
                            LineDelay -= 0.1F
                            If LineDelay <= 0.0F Then
                                LineDelay = 13.0F
                                BroadCastLines.RemoveAt(0)
                            End If
                        End If
                    End If
                End If
            End If

            If Controls.Dismiss(True, True, True) = True Then
                If CurrentStation Is Nothing Then
                    'Play music depending on the player state in the level (surfing and riding):
                    If Screen.Level.Surfing = True Then
                        MusicManager.Play("surf", True) 'Play "surf" when player is surfing.
                    Else
                        If Screen.Level.Riding = True Then
                            MusicManager.Play("ride", True) 'Play "ride" when player is riding.
                        Else
                            MusicManager.Play(Level.MusicLoop, True) 'Play default MusicLoop.
                        End If
                    End If
                End If
                SoundManager.PlaySound("select")
                Me.menuIndex = MenuScreens.Main
            End If
        End Sub

        Private Function GetSelectedStation() As RadioStation
            For Each station As RadioStation In Me.RadioStations
                If station.IsInterfering(RadioCursor) = True Then
                    Return station
                End If
            Next
            Return Nothing
        End Function

        Private Sub CheckForStation()
            CurrentSong = ""
            For Each station As RadioStation In Me.RadioStations
                If station.IsInterfering(RadioCursor) = True Then
                    MusicManager.Play(station.Music, True)
                    CurrentSong = station.Music
                    CurrentStation = station
                    Exit For
                End If
            Next

            If CurrentSong = "" Then
                MusicManager.PlayNoMusic()
                CurrentStation = Nothing
            End If
        End Sub

        Private Sub InitializeRadio()
            Me.InitializedRadio = True

            Dim radioData() As String = System.IO.File.ReadAllLines(GameModeManager.GetContentFilePath("Data\channels.dat"))
            For Each line As String In radioData
                If line.StartsWith("{") = True And line.EndsWith("}") = True Then
                    line = line.Remove(line.Length - 1, 1).Remove(0, 1)

                    Dim r As New RadioStation(line)
                    Me.RadioStations = r.OverwriteChannels(Me.RadioStations)
                End If
            Next

            Me.RadioCursor = Player.Temp.RadioStation
            CheckForStation()

            Logger.Debug("Initialized Radio with " & Me.RadioStations.Count & " stations.")
        End Sub

        Public Shared Function StationCanPlay(ByVal station As RadioStation) As Boolean
            Dim stations As New List(Of RadioStation)

            Dim file As String = GameModeManager.GetContentFilePath("Data\channels.dat")
            Security.FileValidation.CheckFileValid(file, False, "PokegearScreen.vb")

            Dim radioData() As String = System.IO.File.ReadAllLines(file)
            For Each line As String In radioData
                If line.StartsWith("{") = True And line.EndsWith("}") = True Then
                    line = line.Remove(line.Length - 1, 1).Remove(0, 1)

                    Dim r As New RadioStation(line)
                    stations = r.OverwriteChannels(stations)
                End If
            Next

            For Each s As RadioStation In stations
                If s.ChannelMin = station.ChannelMin And s.ChannelMax = station.ChannelMax And s.Name = station.Name Then
                    Return True
                End If
            Next
            Return False
        End Function

#End Region

#Region "TradeRequest"

        Dim TradeRequestNetworkID As Integer = 0
        Dim TradeRequestTexture As Texture2D = Nothing
        Dim TradeRequestName As String = Nothing
        Dim TradeRequestGameJoltID As String = ""

        Dim TradeRequestCursor As Integer = 0

        Private Sub DrawTradeRequest()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 192, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 192), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Trade", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)

            If Not TradeRequestTexture Is Nothing Then
                Core.SpriteBatch.DrawString(FontManager.MainFont, "The player """ & TradeRequestName & """ wants to trade with you.", New Vector2(CInt(startPos.X + 84), CInt(startPos.Y + 80)), Color.Black)

                Dim frameSize As New Size(CInt(TradeRequestTexture.Width / 3), CInt(TradeRequestTexture.Height / 4))
                Dim frameScale As Single = 1.0F
                If TradeRequestTexture.Width = TradeRequestTexture.Height / 2 Then
                    frameSize.Width = CInt(TradeRequestTexture.Width / 2)
                ElseIf TradeRequestTexture.Width = TradeRequestTexture.Height Then
                    frameSize.Width = CInt(TradeRequestTexture.Width / 4)
                Else
                    frameSize.Width = CInt(TradeRequestTexture.Width / 3)
                End If
                If frameSize.Width > 32 Then
                    frameScale = 0.5F
                End If
                Core.SpriteBatch.Draw(TradeRequestTexture, New Rectangle(CInt(startPos.X + 64 - (frameSize.Width * frameScale / 2)), CInt(startPos.Y + 96 - (frameSize.Height * frameScale / 2)), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)

                For i = 0 To 1
                    Dim eff As SpriteEffects = SpriteEffects.None
                    If i = TradeRequestCursor Then
                        eff = SpriteEffects.FlipVertically
                    End If

                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 152 + i * 64), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 152 + i * 64), 192, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 192), CInt(startPos.Y + 152 + i * 64), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                    Dim t As String = "Yes"
                    If i = 1 Then
                        t = "No"
                    End If

                    Core.SpriteBatch.DrawString(FontManager.MainFont, t, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 155 + i * 64)), Color.Black)

                Next
            End If
        End Sub

        Private Sub UpdateTradeRequest()
            Dim startPos As Vector2 = GetStartPosition()

            Dim playerExists As Boolean = False
            For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                If p.ServersID = TradeRequestNetworkID Then
                    playerExists = True
                    Exit For
                End If
            Next
            If playerExists = False Then
                CloseTradeRequest()
            Else
                If TradeRequestTexture Is Nothing Then
                    For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                        If p.ServersID = TradeRequestNetworkID Then
                            Dim t As Texture2D
                            Dim tPath As String = NetworkPlayer.GetTexturePath(p.Skin)
                            If TextureManager.TextureExist(tPath) = True Then
                                t = TextureManager.GetTexture(tPath)
                            Else
                                t = TextureManager.GetTexture("Textures\NPC\0")
                            End If
                            Me.TradeRequestTexture = t
                            Me.TradeRequestName = p.Name
                            Core.StartThreadedSub(AddressOf Me.DownloadTradeRequestSprite)
                        End If
                    Next
                Else
                    If Controls.Down(True, True) = True Then
                        Me.TradeRequestCursor += 1
                    End If
                    If Controls.Up(True, True) = True Then
                        Me.TradeRequestCursor -= 1
                    End If

                    Me.TradeRequestCursor = Me.TradeRequestCursor.Clamp(0, 1)

                    If Controls.Accept(True, False, False) = True Then
                        For i = 0 To 1
                            If New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 152 + i * 64), 224, 32).Contains(MouseHandler.MousePosition) = True Then
                                If i = Me.TradeRequestCursor Then
                                    Select Case i
                                        Case 0
                                            SoundManager.PlaySound("select")
                                            CloseTradeRequest()
                                            Core.SetScreen(New DirectTradeScreen(Core.CurrentScreen, TradeRequestNetworkID, False))
                                        Case 1
                                            SoundManager.PlaySound("select")
                                            CloseTradeRequest()
                                    End Select
                                Else
                                    Me.TradeRequestCursor = i
                                End If
                            End If
                        Next
                    End If

                    If Controls.Accept(False, True, True) = True Then
                        Select Case Me.TradeRequestCursor
                            Case 0
                                SoundManager.PlaySound("select")
                                CloseTradeRequest()
                                Core.SetScreen(New DirectTradeScreen(Core.CurrentScreen, TradeRequestNetworkID, False))
                            Case 1
                                SoundManager.PlaySound("select")
                                CloseTradeRequest()
                        End Select
                    End If
                    If Controls.Dismiss() = True Then
                        SoundManager.PlaySound("select")
                        CloseTradeRequest()
                    End If
                End If
            End If
        End Sub

        Private Sub DownloadTradeRequestSprite()
            If Me.TradeRequestGameJoltID <> "" Then
                Dim t As Texture2D = Emblem.GetOnlineSprite(Me.TradeRequestGameJoltID)
                If Not t Is Nothing Then
                    Me.TradeRequestTexture = t
                End If
            End If
        End Sub

        Private Sub CloseTradeRequest()
            Core.SetScreen(Me.PreScreen)
        End Sub

#End Region

#Region "BattleRequest"

        Dim BattleRequestNetworkID As Integer = 0
        Dim BattleRequestTexture As Texture2D = Nothing
        Dim BattleRequestName As String = Nothing
        Dim BattleRequestGameJoltID As String = ""

        Dim BattleRequestCursor As Integer = 0

        Private Sub DrawBattleRequest()
            Dim startPos As Vector2 = GetStartPosition()

            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 46), 16, 32), New Rectangle(96, 112, 8, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 46), 192, 32), New Rectangle(102, 112, 4, 16), Color.White)
            Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 192), CInt(startPos.Y + 46), 16, 32), New Rectangle(104, 112, 8, 16), Color.White)

            Core.SpriteBatch.DrawString(FontManager.MainFont, "Battle", New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 48)), Color.Black)

            If Not BattleRequestTexture Is Nothing Then
                Core.SpriteBatch.DrawString(FontManager.MainFont, "The player """ & BattleRequestName & """ wants to battle with you.", New Vector2(CInt(startPos.X + 84), CInt(startPos.Y + 80)), Color.Black)

                Dim frameSize As New Size(CInt(BattleRequestTexture.Width / 3), CInt(BattleRequestTexture.Height / 4))
                Dim frameScale As Single = 1.0F
                If BattleRequestTexture.Width = BattleRequestTexture.Height / 2 Then
                    frameSize.Width = CInt(BattleRequestTexture.Width / 2)
                ElseIf BattleRequestTexture.Width = BattleRequestTexture.Height Then
                    frameSize.Width = CInt(BattleRequestTexture.Width / 4)
                Else
                    frameSize.Width = CInt(BattleRequestTexture.Width / 3)
                End If
                If frameSize.Width > 32 Then
                    frameScale = 0.5F
                End If

                Core.SpriteBatch.Draw(BattleRequestTexture, New Rectangle(CInt(startPos.X + 64 - (frameSize.Width * frameScale / 2)), CInt(startPos.Y + 96 - (frameSize.Height * frameScale / 2)), CInt(frameSize.Width * frameScale), CInt(frameSize.Height * frameScale)), New Rectangle(0, frameSize.Height * 2, frameSize.Width, frameSize.Height), Color.White)

                For i = 0 To 1
                    Dim eff As SpriteEffects = SpriteEffects.None
                    If i = BattleRequestCursor Then
                        eff = SpriteEffects.FlipVertically
                    End If

                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 152 + i * 64), 16, 32), New Rectangle(96, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16), CInt(startPos.Y + 152 + i * 64), 192, 32), New Rectangle(102, 112, 4, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)
                    Core.SpriteBatch.Draw(TextureManager.GetTexture("GUI\Menus\pokegear"), New Rectangle(CInt(startPos.X + 40 + 16 + 192), CInt(startPos.Y + 152 + i * 64), 16, 32), New Rectangle(104, 112, 8, 16), Color.White, 0.0F, Vector2.Zero, eff, 0.0F)

                    Dim t As String = "Yes"
                    If i = 1 Then
                        t = "No"
                    End If

                    Core.SpriteBatch.DrawString(FontManager.MainFont, t, New Vector2(CInt(startPos.X + 48), CInt(startPos.Y + 155 + i * 64)), Color.Black)
                Next
            End If
        End Sub

        Private Sub UpdateBattleRequest()
            Dim startPos As Vector2 = GetStartPosition()

            Dim playerExists As Boolean = False
            For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                If p.ServersID = BattleRequestNetworkID Then
                    playerExists = True
                    Exit For
                End If
            Next
            If playerExists = False Then
                CloseBattleRequest()
            Else
                If BattleRequestTexture Is Nothing Then
                    For Each p As Servers.Player In Core.ServersManager.PlayerCollection
                        If p.ServersID = BattleRequestNetworkID Then
                            Dim t As Texture2D
                            Dim tPath As String = NetworkPlayer.GetTexturePath(p.Skin)
                            If TextureManager.TextureExist(tPath) = True Then
                                t = TextureManager.GetTexture(tPath)
                            Else
                                t = TextureManager.GetTexture("Textures\NPC\0")
                            End If
                            Me.BattleRequestTexture = t
                            Me.BattleRequestName = p.Name
                            Core.StartThreadedSub(AddressOf Me.DownloadBattleRequestSprite)
                        End If
                    Next
                Else
                    If Controls.Down(True, True) = True Then
                        Me.BattleRequestCursor += 1
                    End If
                    If Controls.Up(True, True) = True Then
                        Me.BattleRequestCursor -= 1
                    End If

                    Me.BattleRequestCursor = Me.BattleRequestCursor.Clamp(0, 1)

                    If Controls.Accept(True, False, False) = True Then
                        For i = 0 To 1
                            If New Rectangle(CInt(startPos.X + 40), CInt(startPos.Y + 152 + i * 64), 224, 32).Contains(MouseHandler.MousePosition) = True Then
                                If i = Me.BattleRequestCursor Then
                                    Select Case i
                                        Case 0
                                            SoundManager.PlaySound("select")
                                            CloseBattleRequest()
                                            Core.SetScreen(New PVPLobbyScreen(Core.CurrentScreen, BattleRequestNetworkID, False))
                                        Case 1
                                            SoundManager.PlaySound("select")
                                            CloseBattleRequest()
                                    End Select
                                Else
                                    Me.BattleRequestCursor = i
                                End If
                            End If
                        Next
                    End If

                    If Controls.Accept(False, True, True) = True Then
                        Select Case Me.BattleRequestCursor
                            Case 0
                                SoundManager.PlaySound("select")
                                CloseBattleRequest()
                                Core.SetScreen(New PVPLobbyScreen(Core.CurrentScreen, BattleRequestNetworkID, False))
                            Case 1
                                SoundManager.PlaySound("select")
                                CloseBattleRequest()
                        End Select
                    End If
                    If Controls.Dismiss() = True Then
                        SoundManager.PlaySound("select")
                        CloseBattleRequest()
                    End If
                End If
            End If
        End Sub

        Private Sub DownloadBattleRequestSprite()
            If Me.BattleRequestGameJoltID <> "" Then
                Dim t As Texture2D = Emblem.GetOnlineSprite(Me.BattleRequestGameJoltID)
                If Not t Is Nothing Then
                    Me.BattleRequestTexture = t
                End If
            End If
        End Sub

        Private Sub CloseBattleRequest()
            Core.SetScreen(Me.PreScreen)
        End Sub

#End Region

        Private Function GetStartPosition() As Vector2
            Return New Vector2(CInt(Core.windowSize.Width / 2 - width / 2), CInt(Core.windowSize.Height - heigth))
        End Function

    End Class

End Namespace