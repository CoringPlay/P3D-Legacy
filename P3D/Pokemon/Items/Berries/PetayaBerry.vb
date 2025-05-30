Namespace Items.Berries

    <Item(2055, "Petaya")>
    Public Class PetayaBerry

        Inherits Berry
        Public Overrides ReadOnly Property CanBeUsed As Boolean = False
        Public Overrides ReadOnly Property CanBeUsedInBattle As Boolean = False
        Public Sub New()
            MyBase.New(86400, "A Berry to be consumed by Pokémon. If a Pokémon holds one, its Sp. Atk. stat will increase when it's in a pinch.", "23.7cm", "Very Hard", 1, 2)

            Me.Spicy = 30
            Me.Dry = 0
            Me.Sweet = 0
            Me.Bitter = 30
            Me.Sour = 10

            Me.Type = Element.Types.Poison
            Me.Power = 100
            Me.JuiceColor = "pink"
            Me.JuiceGroup = 3
        End Sub

    End Class

End Namespace
