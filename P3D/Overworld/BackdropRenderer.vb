﻿Public Class BackdropRenderer

    Private _backdrops As New List(Of Backdrop)

    Public Sub Initialize()
    End Sub

    Public Sub Clear()
        Me._backdrops.Clear()
    End Sub

    Public Sub AddBackdrop(ByVal Backdrop As Backdrop)
        Me._backdrops.Add(Backdrop)
    End Sub

    Public Sub Update()
        For Each b As Backdrop In Me._backdrops
            b.Update()
        Next
    End Sub

    Public Sub Draw()
        Dim tempRasterizer = GraphicsDevice.RasterizerState

        GraphicsDevice.RasterizerState = RasterizerState.CullNone
        GraphicsDevice.SamplerStates(0) = New SamplerState() With {.Filter = TextureFilter.Point, .AddressU = TextureAddressMode.Wrap, .AddressV = TextureAddressMode.Wrap}

        For Each b As Backdrop In Me._backdrops
            b.Draw({0, 1, 3, 2, 3, 0})
        Next

        GraphicsDevice.RasterizerState = tempRasterizer
        GraphicsDevice.SamplerStates(0) = Core.Sampler
    End Sub

    Public Class Backdrop

        Structure VertexPositionNormalTangentTexture

            Public pos As Vector3
            Public uv As Vector2
            Public normal As Vector3
            Public tangent As Vector3

            Public Sub New(ByVal position As Vector3, ByVal nor As Vector3, ByVal tan As Vector3, ByVal texturePosition As Vector2)
                Me.pos = position
                Me.normal = nor
                Me.tangent = tan
                Me.uv = texturePosition
            End Sub

            Public Shared Function SizeInBytes() As Integer
                Return 4 * (3 + 2 + 3 + 3)
            End Function

            Public Shared VertexElements As VertexElement() = {New VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                 New VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                 New VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                 New VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0)}

            Public Shared VertexDeclaration As VertexDeclaration = New VertexDeclaration(VertexElements)

        End Structure

        Public Enum BackdropTypes
            Water
            Grass
            Texture
            Animation
        End Enum

        Private _vertices As New List(Of VertexPositionNormalTangentTexture)
        Private _backdropType As BackdropTypes = BackdropTypes.Grass
        Private _backdropTexture As Texture2D = Nothing
        Private _customTexture As Texture2D = Nothing
        Private _worldMatrix As Matrix = Matrix.Identity
        Private _position As Vector3
        Private _rotation As Vector3
        Private _shader As Effect
        Private _width As Integer = 0
        Private _height As Integer = 0

        Dim WaterAnimation As Animation
        Dim CustomAnimation As Animation

        Private _waterAnimationDelay As Single = CSng(1 / GameModeManager.ActiveGameMode.WaterSpeed)
        Private _waterAnimationIndex As Integer = 0

        Private _setTexture As Boolean = False

        Public Sub New(ByVal BackdropType As String, ByVal Position As Vector3, ByVal Rotation As Vector3, ByVal Width As Integer, ByVal Height As Integer)
            Me.New(BackdropType, Position, Rotation, Width, Height, Nothing)
        End Sub

        Public Sub New(ByVal BackdropType As String, ByVal Position As Vector3, ByVal Rotation As Vector3, ByVal Width As Integer, ByVal Height As Integer, ByVal BackdropTexture As Texture2D, Optional ByVal AnimationSpeed As Integer = Nothing, Optional FrameCount As Integer = Nothing)
            _shader = Content.Load(Of Effect)("Effects\BackdropShader")

            _vertices.Add(New VertexPositionNormalTangentTexture(New Vector3(0, 0, 0), New Vector3(-1, 0, 0), New Vector3(0, 1, 0), New Vector2(0, 0)))
            _vertices.Add(New VertexPositionNormalTangentTexture(New Vector3(Width, 0, 0), New Vector3(-1, 0, 0), New Vector3(0, 1, 0), New Vector2(1, 0)))
            _vertices.Add(New VertexPositionNormalTangentTexture(New Vector3(0, 0, Height), New Vector3(-1, 0, 0), New Vector3(0, 1, 0), New Vector2(0, 1)))
            _vertices.Add(New VertexPositionNormalTangentTexture(New Vector3(Width, 0, Height), New Vector3(-1, 0, 0), New Vector3(0, 1, 0), New Vector2(1, 1)))

            Me._position = Position
            Me._rotation = Rotation
            Me._backdropTexture = BackdropTexture

            Me._width = Width
            Me._height = Height

            Select Case BackdropType.ToLower()
                Case "water"
                    Me._backdropType = BackdropTypes.Water
                    Dim WaterSize As Size = New Size(CInt(TextureManager.GetTexture("Textures\Backdrops\Water").Width / 3), CInt(TextureManager.GetTexture("Textures\Backdrops\Water").Height))
                    WaterAnimation = New Animation(TextureManager.GetTexture("Textures\Backdrops\Water"), 1, 3, WaterSize.Width, WaterSize.Height, GameModeManager.ActiveGameMode.WaterSpeed, 0, 0)
                    _backdropTexture = TextureManager.GetTexture("Textures\Backdrops\Water", WaterAnimation.TextureRectangle, "")
                Case "grass"
                    Me._backdropType = BackdropTypes.Grass
                Case "texture"
                    Me._backdropType = BackdropTypes.Texture
                Case "animation"
                    Me._backdropType = BackdropTypes.Animation
                    _customTexture = BackdropTexture
                    Dim _animationspeed As Integer
                    Dim _frameCount As Integer

                    If AnimationSpeed = Nothing Then
                        _animationspeed = GameModeManager.ActiveGameMode.WaterSpeed
                    Else
                        _animationspeed = AnimationSpeed
                    End If

                    If FrameCount = Nothing Then
                        _frameCount = 3
                    Else
                        _frameCount = FrameCount
                    End If

                    Dim AnimationSize As Size = New Size(CInt(_customTexture.Width / _frameCount), CInt(_customTexture.Height))
                    CustomAnimation = New Animation(_customTexture, 1, _frameCount, AnimationSize.Width, AnimationSize.Height, _animationspeed, 0, 0)
                    _backdropTexture = TextureManager.GetTexture(_customTexture, CustomAnimation.TextureRectangle)

            End Select

            Me.Update()
        End Sub

        Public Sub Update()
            _worldMatrix = Matrix.CreateFromYawPitchRoll(Me._rotation.Y, Me._rotation.X, Me._rotation.Z) * Matrix.CreateTranslation(Me._position)

            Select Case Me._backdropType
                Case BackdropTypes.Water
                    If Core.GameOptions.GraphicStyle = 1 Then
                        If Not WaterAnimation Is Nothing Then
                            WaterAnimation.Update(0.005)
                            _backdropTexture = TextureManager.GetTexture("Textures\Backdrops\Water", WaterAnimation.TextureRectangle, "")
                        End If
                    End If
                Case BackdropTypes.Grass
                    If Me._setTexture = False Then
                        Dim GrassSize As Size = New Size(CInt(TextureManager.GetTexture("Textures\Backdrops\Grass").Width / 4), CInt(TextureManager.GetTexture("Textures\Backdrops\Grass").Height))
                        Dim x As Integer = 0

                        Select Case World.CurrentSeason
                            Case World.Seasons.Winter
                                x = 0
                            Case World.Seasons.Spring
                                x = GrassSize.Width
                            Case World.Seasons.Summer
                                x = GrassSize.Width * 2
                            Case World.Seasons.Fall
                                x = GrassSize.Width * 3
                        End Select

                        _backdropTexture = TextureManager.GetTexture("Backdrops\Grass", New Rectangle(x, 0, GrassSize.Width, GrassSize.Height))
                        Me._setTexture = True
                    End If
                Case BackdropTypes.Animation
                    If Core.GameOptions.GraphicStyle = 1 Then
                        If Not CustomAnimation Is Nothing Then
                            CustomAnimation.Update(0.005)
                            _backdropTexture = TextureManager.GetTexture(_customTexture, CustomAnimation.TextureRectangle)
                        End If
                    Else
                        If Me._setTexture = False Then
                            _backdropTexture = TextureManager.GetTexture(_customTexture, CustomAnimation.TextureRectangle)
                            Me._setTexture = True
                        End If
                    End If

            End Select
        End Sub

        Public Sub Draw(ByVal Indicies As Short())
            Dim vBuffer As New VertexBuffer(Core.GraphicsDevice, VertexPositionNormalTangentTexture.VertexDeclaration, _vertices.Count, BufferUsage.None)
            Dim iBuffer As New IndexBuffer(Core.GraphicsDevice, GetType(Short), Indicies.Count, BufferUsage.None)

            vBuffer.SetData(Of VertexPositionNormalTangentTexture)(_vertices.ToArray())
            iBuffer.SetData(Of Short)(Indicies)

            _shader.Parameters("World").SetValue(_worldMatrix)
            _shader.CurrentTechnique = _shader.Techniques("Texture")
            _shader.Parameters("View").SetValue(Screen.Camera.View)
            _shader.Parameters("Projection").SetValue(Screen.Camera.Projection)
            _shader.Parameters("DiffuseColor").SetValue(GetDiffuseColor())
            _shader.Parameters("TexStretch").SetValue(New Vector2(Me._width, Me._height))
            _shader.Parameters("color").SetValue(_backdropTexture)

            For Each pass As EffectPass In _shader.CurrentTechnique.Passes
                pass.Apply()
                GraphicsDevice.SetVertexBuffer(vBuffer)
                GraphicsDevice.Indices = iBuffer
                'GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertices.Count, 0, Indicies.Count)
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertices.Count)
            Next

            vBuffer.Dispose()
            iBuffer.Dispose()
        End Sub

        Private Function GetDiffuseColor() As Vector4
            Dim dayColor As Vector3 = Vector3.One
            Dim diffuseColor As Vector3 = Screen.Effect.DiffuseColor

            If Not Screen.Level.World Is Nothing Then
                Select Case Screen.Level.World.EnvironmentType
                    Case P3D.World.EnvironmentTypes.Outside
                        If Core.GameOptions.LightingEnabled = True Then
                            dayColor = SkyDome.GetDaytimeColor(True).ToVector3()
                        End If
                    Case P3D.World.EnvironmentTypes.Dark
                        dayColor = New Vector3(0.5F, 0.5F, 0.5F)
                End Select
            End If


            If Core.GameOptions.LightingEnabled = True Then
                Return (dayColor * diffuseColor * Lighting.GetEnvironmentColor(1)).ToColor().ToVector4()
            Else
                Return (dayColor * diffuseColor).ToColor().ToVector4()
            End If
        End Function

    End Class

End Class