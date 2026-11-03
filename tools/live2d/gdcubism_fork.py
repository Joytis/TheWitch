"""The Witch's fork of GDCubism: scripted transforms over a fresh upstream clone.

Shared by tools/live2d/setup-live2d.ps1 (local Windows build) and
.github/workflows/live2d-build.yml (Windows/Linux/macOS CI build), so every platform's binary
and the shipped shaders come from the same source. Fails loudly when upstream no longer matches
what a transform expects, rather than building something that collides or misrenders.

Transforms:
  - every GDCubism* class -> WitchCubism* (another mod's stock GDCubism can't collide with ours:
    Godot keeps whichever extension registers a class name first)
  - shaders loaded from res://TheWitch/live2d/shader/ instead of res://addons/gd_cubism/ (pcks
    loaded later override earlier res:// paths)
  - library named libwitch_cubism.* (SConstruct names it after the demo's *.gdextension)
  - parts drawn in sibling order instead of z_index, so they can't draw over the game's UI
  - unmasked Multiply parts actually multiply (upstream's norm_mul shader is a copy of norm_mix)
  - WitchCubismEffectBreath gets an `intensity` property scaling its (hard-coded) swings

Bump FORK_REV whenever a transform changes; setup-live2d.ps1 then re-clones and rebuilds.

Usage (Python 3.8+, stdlib only):
  python gdcubism_fork.py --rev            # print FORK_REV
  python gdcubism_fork.py <gd_cubism checkout>
"""
import re
import sys
from pathlib import Path

FORK_REV = "4"
EXT_NAME = "witch_cubism"
# Must match Live2DRuntime.ShaderResDir.
SHADER_RES = "res://TheWitch/live2d/shader/"
UPSTREAM_SHADER_RES = "res://addons/gd_cubism/res/shader/"


class ForkError(Exception):
    pass


def read(path):
    return path.read_text(encoding="utf-8").replace("\r\n", "\n")


def write(path, text):
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(text, old, new, where):
    hits = text.count(old)
    if hits != 1:
        raise ForkError(f"expected 1 match of {old.strip()!r} in {where}, found {hits}; upstream changed")
    return text.replace(old, new)


def rename_classes_and_shader_paths(src):
    shader_hits = 0
    for path in sorted((src / "src").rglob("*")):
        if path.suffix not in (".cpp", ".hpp", ".h"):
            continue
        text = read(path)
        new = re.sub(r"\bGDCubism(?=[A-Z])", "WitchCubism", text)
        shader_hits += new.count(UPSTREAM_SHADER_RES)
        new = new.replace(UPSTREAM_SHADER_RES, SHADER_RES)
        if re.search(r"GDCubism[A-Z]", new):
            raise ForkError(f"missed a GDCubism class name in {path}")
        if new != text:
            write(path, new)
    if shader_hits == 0:
        raise ForkError("found no shader paths to relocate; upstream changed")


def draw_order_by_siblings(src):
    # Upstream sets each part's z_index to its Cubism render order (0..N). z_index is relative to
    # the model, not contained by it, so parts sort against the rest of the scene and draw over UI
    # at z 0 (pause overlay, popups). Order the part nodes as siblings instead (only when the order
    # changed) and leave z_index at 0, so the model draws as one block in tree order.
    path = src / "src/private/internal_cubism_renderer_2d.cpp"
    text = read(path)
    text = replace_once(text, "#include <cfloat>\n", """#include <cfloat>
#include <algorithm>
#include <utility>
#include <vector>
""", path)
    text = replace_once(text, "    // update meshes\n", """    // Witch fork: draw order by sibling order, not z_index (see tools/live2d/gdcubism_fork.py).
    std::vector<std::pair<Csm::csmInt32, MeshInstance2D *>> witch_draw_order;

    // update meshes
""", path)
    text = replace_once(text, "        node->set_z_index(renderOrder[index]);\n", """        witch_draw_order.emplace_back(renderOrder[index], node);
""", path)
    text = replace_once(text, "    // update masks\n", """    std::sort(witch_draw_order.begin(), witch_draw_order.end(),
              [](const auto &a, const auto &b) { return a.first < b.first; });
    for (size_t i = 1; i < witch_draw_order.size(); i++) {
        if (witch_draw_order[i - 1].second->get_index() > witch_draw_order[i].second->get_index()) {
            for (auto &entry : witch_draw_order) {
                entry.second->get_parent()->move_child(entry.second, -1);
            }
            break;
        }
    }

    // update masks
""", path)
    write(path, text)


def multiply_blend(src):
    # Upstream's unmasked Multiply shader is a verbatim copy of the Normal one (premultiplied alpha
    # blend), so Multiply parts paint their own colour on top instead of darkening what's below
    # (seen as a blue tint on the Witch). Use the Cubism multiply formula, as the masked variant
    # (2d_cubism_mask_mul) already does: dst * (src.rgb * src.a + (1 - src.a)).
    path = src / "demo/addons/gd_cubism/res/shader/2d_cubism_norm_mul.gdshader"
    text = read(path)
    text = replace_once(text, "render_mode blend_premul_alpha, unshaded;", "render_mode blend_mul, unshaded;", path)
    text = replace_once(
        text,
        "    COLOR = vec4(color.rgb * color.a, color.a);",
        "    // Witch fork: real multiply (see tools/live2d/gdcubism_fork.py).\n"
        "    COLOR = vec4(color.rgb * color.a + (1.0 - color.a), 1.0);",
        path,
    )
    write(path, text)


def breath_intensity(src):
    # Upstream's breath effect hard-codes the stock Cubism breath curves (head sway on
    # ParamAngleX/Y/Z, ParamBodyAngleX, ParamBreath). Expose `intensity` (default 1 = upstream)
    # multiplying every curve's peak, settable any time (re-applies the curves).
    path = src / "src/gd_cubism_effect_breath.hpp"
    text = read(path)
    text = replace_once(text, """protected:
    static void _bind_methods() {}

private:
    Csm::CubismBreath* _breath = nullptr;
""", """protected:
    static void _bind_methods() {
        ClassDB::bind_method(D_METHOD("set_intensity", "intensity"), &WitchCubismEffectBreath::set_intensity);
        ClassDB::bind_method(D_METHOD("get_intensity"), &WitchCubismEffectBreath::get_intensity);
        ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "intensity"), "set_intensity", "get_intensity");
    }

private:
    Csm::CubismBreath* _breath = nullptr;
    // Witch fork: multiplies every curve's peak; 1 = upstream (see tools/live2d/gdcubism_fork.py).
    float _intensity = 1.0f;

    void witch_set_breath_parameters() {
        const float k = this->_intensity;
        Csm::csmVector<Csm::CubismBreath::BreathParameterData> param;
        param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleX), 0.0f, 15.0f * k, 6.5345f, 0.5f));
        param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleY), 0.0f, 8.0f * k, 3.5345f, 0.5f));
        param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleZ), 0.0f, 10.0f * k, 5.5345f, 0.5f));
        param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamBodyAngleX), 0.0f, 4.0f * k, 15.5345f, 0.5f));
        param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamBreath), 0.5f, 0.5f * k, 3.2345f, 0.5f));
        this->_breath->SetParameters(param);
    }
""", path)
    text = replace_once(text, """public:
    virtual void _cubism_init(""", """public:
    void set_intensity(const float intensity) {
        this->_intensity = intensity;
        if(this->_breath != nullptr) this->witch_set_breath_parameters();
    }
    float get_intensity() const { return this->_intensity; }

    virtual void _cubism_init(""", path)
    text = replace_once(text, """            Csm::csmVector<Csm::CubismBreath::BreathParameterData> param;

            param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleX), 0.0f, 15.0f, 6.5345f, 0.5f));
            param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleY), 0.0f, 8.0f, 3.5345f, 0.5f));
            param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamAngleZ), 0.0f, 10.0f, 5.5345f, 0.5f));
            param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamBodyAngleX), 0.0f, 4.0f, 15.5345f, 0.5f));
            param.PushBack(Csm::CubismBreath::BreathParameterData(CubismFramework::GetIdManager()->GetId(ParamBreath), 0.5f, 0.5f, 3.2345f, 0.5f));

            this->_breath->SetParameters(param);
""", """            this->witch_set_breath_parameters();
""", path)
    write(path, text)


def rename_extension(src):
    # SConstruct names the library after the single demo/addons/*/*.gdextension.
    ext = src / "demo/addons/gd_cubism/gd_cubism.gdextension"
    if not ext.is_file():
        raise ForkError(f"{ext} not found; upstream changed")
    ext.rename(ext.with_name(f"{EXT_NAME}.gdextension"))


def apply(src):
    rename_classes_and_shader_paths(src)
    draw_order_by_siblings(src)
    multiply_blend(src)
    breath_intensity(src)
    rename_extension(src)


def main(argv):
    if argv == ["--rev"]:
        print(FORK_REV)
        return 0
    if len(argv) != 1:
        print(__doc__, file=sys.stderr)
        return 2
    try:
        apply(Path(argv[0]))
    except ForkError as e:
        print(f"gdcubism_fork: {e}", file=sys.stderr)
        return 1
    print(f"gdcubism_fork: applied Witch fork rev {FORK_REV} to {argv[0]}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
