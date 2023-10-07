﻿using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectImage : DreamObject {
    public IconAppearance? Appearance;

    private DreamObject? _loc;
    private DreamList _overlays;
    private DreamList _underlays;

    /// <summary>
    /// All the args in /image/New() after "icon" and "loc", in their correct order
    /// </summary>
    private static readonly string[] IconCreationArgs = {
        "icon_state",
        "layer",
        "dir",
        "pixel_x",
        "pixel_y"
    };

    public DreamObjectImage(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        if (objectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
            // /mutable_appearance.overlays and /mutable_appearance.underlays are normal lists
            _overlays = ObjectTree.CreateList();
            _underlays = ObjectTree.CreateList();
        } else {
            _overlays = new DreamOverlaysList(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, false);
            _underlays = new DreamOverlaysList(ObjectTree.List.ObjectDefinition, this, AppearanceSystem, true);
        }
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        DreamValue icon = args.GetArgument(0);
        if (!AtomManager.TryCreateAppearanceFrom(icon, out Appearance)) {
            // Use a default appearance, but log a warning about it if icon wasn't null
            Appearance = new IconAppearance();
            if (!icon.IsNull)
                Logger.GetSawmill("opendream.image")
                    .Warning($"Attempted to create an /image from {icon}. This is invalid and a default image was created instead.");
        }

        int startIndex = 0;
        DreamValue loc = args.GetArgument(1);
        if (!loc.TryGetValueAsDreamObject(out _loc)) { // If it's not a DreamObject, it's actually icon_state and not loc
            startIndex = 1;
            if (!loc.IsNull) {
                AtomManager.SetAppearanceVar(Appearance, "icon_state", loc);
            }
        }

        for (int i = startIndex; i < IconCreationArgs.Length; i++) {
            var arg = args.GetArgument(i + 2);
            if (arg.IsNull)
                continue;

            string argName = IconCreationArgs[i];
            AtomManager.SetAppearanceVar(Appearance, argName, arg);
            if (argName == "dir") {
                // If a dir is explicitly given in the constructor then overlays using this won't use their owner's dir
                // Setting dir after construction does not affect this
                // This is undocumented and I hate it
                Appearance.InheritsDirection = false;
            }
        }
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        // TODO: filters, transform
        switch(varName) {
            case "appearance": {
                IconAppearance appearanceCopy = new IconAppearance(Appearance!); // Return a copy
                value = new(appearanceCopy);
                return true;
            }
            case "loc": {
                value = new(_loc);
                return true;
            }
            case "overlays":
                value = new(_overlays);
                return true;
            case "underlays":
                value = new(_underlays);
                return true;
            default: {
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    value = AtomManager.GetAppearanceVar(Appearance!, varName);
                    return true;
                } else {
                    return base.TryGetVar(varName, out value);
                }
            }
        }
    }


    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "appearance":
                if (!AtomManager.TryCreateAppearanceFrom(value, out var newAppearance))
                    return; // Ignore attempts to set an invalid appearance

                // The dir does not get changed
                var oldDir = Appearance!.Direction;
                newAppearance.Direction = oldDir;

                Appearance = newAppearance;
                break;
            case "loc":
                value.TryGetValueAsDreamObject(out _loc);
                break;
            case "overlays": {
                value.TryGetValueAsDreamList(out var valueList);

                // /mutable_appearance has some special behavior for its overlays and underlays vars
                // They're normal lists, not the special DreamOverlaysList.
                // Setting them to a list will create a copy of that list.
                // Otherwise it attempts to create an appearance and creates a new (normal) list with that appearance
                if (ObjectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
                    if (valueList != null) {
                        _overlays = valueList.CreateCopy();
                    } else {
                        var overlay = DreamOverlaysList.CreateOverlayAppearance(AtomManager, value, Appearance?.Icon);
                        if (overlay == null)
                            return;

                        _overlays.Cut();
                        _overlays.AddValue(new(overlay));
                    }

                    return;
                }

                _overlays.Cut();

                if (valueList != null) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue overlayValue in valueList.GetValues()) {
                        _overlays.AddValue(overlayValue);
                    }
                } else if (!value.IsNull) {
                    _overlays.AddValue(value);
                }

                break;
            }
            case "underlays": {
                value.TryGetValueAsDreamList(out var valueList);

                // See the comment in the overlays setter for info on this
                if (ObjectDefinition.IsSubtypeOf(ObjectTree.MutableAppearance)) {
                    if (valueList != null) {
                        _underlays = valueList.CreateCopy();
                    } else {
                        var underlay = DreamOverlaysList.CreateOverlayAppearance(AtomManager, value, Appearance?.Icon);
                        if (underlay == null)
                            return;

                        _underlays.Cut();
                        _underlays.AddValue(new(underlay));
                    }

                    return;
                }

                _underlays.Cut();

                if (valueList != null) {
                    // TODO: This should postpone UpdateAppearance until after everything is added
                    foreach (DreamValue underlayValue in valueList.GetValues()) {
                        _underlays.AddValue(underlayValue);
                    }
                } else if (!value.IsNull) {
                    _underlays.AddValue(value);
                }

                break;
            }
            case "override": {
                Appearance!.Override = value.IsTruthy();
                break;
            }
            default:
                if (AtomManager.IsValidAppearanceVar(varName)) {
                    AtomManager.SetAppearanceVar(Appearance!, varName, value);
                    break;
                }

                base.SetVar(varName, value);
                break;
        }
    }

    public DreamObject? GetAttachedLoc(){
        return this._loc;
    }
}
