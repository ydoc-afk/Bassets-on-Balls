using System.Collections.Generic;
using System.Linq;
using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;
using WolfUI.Misc;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class Profile : Button, IRestorable<Profile>
{
    [Signal]
    private delegate void EnteredViewEventHandler();
    private const float IconInset = 14f; // matches ProfileCard in the theme
    private bool _wasInView = false;
    //public Profile profile;
    
    public static Profile Restore()
    {
        return Create();
    }

    public static Profile Create(string name)
    {
        var obj = Create();
        obj.Name = name;
        return obj;
    }
#nullable disable
    private Profile() { }
#nullable enable

    public override void _Ready()
    {
        NameLabel.Text = Name;
        NameLabel.Text = ProfileName;
        
        if (Engine.IsEditorHint())
        {
            return;
        }

        var iconMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/rounded_texture.gdshader") };
        iconMaterial.SetShaderParameter("radius", 26.0f);
        Material = iconMaterial;
        void FitIcon() => iconMaterial.SetShaderParameter("rect_size", Size - new Vector2(IconInset, IconInset) * 2f);
        Resized += FitIcon;
        FitIcon();

        CardMotion.Attach(this, this, GetNodeOrNull<GlassPanel>("Glass"));

        Pressed += OnPressed;
        EnteredView += OnEnteredView;
    }

    private async void OnEnteredView()
    {
        if (IconPngPath is not null && IconPngPath != "")
            Icon = await WolfApi.GetIcon(IconPngPath);
    }
    
    private async void OnPressed()
    {
        if (Pin is not null)
        {
            var focus = GetViewport().GuiGetFocusOwner();
            var pin = await PinInput.RequestPin();
            if (!pin.SequenceEqual(Pin))
            {
                await QuestionDialogue.OpenDialogue<bool>(
                    "Incorrect Pin",
                    "The entered Pin is incorrect",
                    new Dictionary<string, bool>
                    {
                        {"OK", false}
                    });
                focus.GrabFocus();
                return;
            }
        }

        WolfApi.ActiveProfile = this;

        if(Main.Singleton.AppList is AppList appMenu)
            appMenu.Visible = true;
    }
    
    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
		{
			return;
		}


        if (_wasInView || !GetGlobalRect().
                Intersection(Main.Singleton.GetNode<Control>("%UserList").GetGlobalRect())
                .HasArea()) return;
        
        EmitSignalEnteredView();
        _wasInView = true;
    }


}
