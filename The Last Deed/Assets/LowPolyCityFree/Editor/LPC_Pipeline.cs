// Low Poly City - render pipeline setup.
//
// The pack's three materials ship on Unity's built-in Standard shader,
// because that is the only shader whose GUID is fixed for every install
// and can therefore be written into a .mat file by a build script that
// has never seen the buyer's project. In URP or HDRP that shader does not
// exist and every model imports magenta.
//
// So the materials are RE-SHADED here, in the project, where Shader.Find
// can actually see what is installed. It runs by itself when the pack's
// materials are found on a shader that does not belong to the active
// pipeline, and can be re-run by hand at any time:
//
//   Tools > Low Poly City > Set Up Materials For Current Pipeline
//
// THE METAL/SMOOTHNESS ATLAS. M_LPC_Main carries a second atlas,
// LPC_Palette_MetalSmooth, with the same cell layout as the colour atlas:
// R = metallic, G = 255, B = 0, A = smoothness. It is ONE texture that all
// three pipelines read, under different names:
//
//   Built-in Standard  _MetallicGlossMap, keyword _METALLICGLOSSMAP,
//                      _SmoothnessTextureChannel 0 (metallic alpha),
//                      _GlossMapScale 1
//   URP Lit            _MetallicGlossMap, keyword _METALLICSPECGLOSSMAP,
//                      _WorkflowMode 1 (metallic), smoothness from the
//                      map's alpha scaled by _Smoothness - so 1
//   HDRP Lit           _MaskMap, keyword _MASKMAP: R metallic, G ambient
//                      occlusion, B detail mask, A smoothness. That is why
//                      G is 255 - a G of 0 would read as "fully occluded"
//                      and render the whole pack near-black. Metallic and
//                      smoothness come from the map through the
//                      _MetallicRemap / _SmoothnessRemap ranges, set 0..1.
//
// A shader swap drops every property the new shader does not declare, so
// the map is read BEFORE the swap under whichever name the old shader had,
// and if something else (Unity's own material upgrader, say) has already
// lost it, it is reloaded from the pack by path. Without it every iron
// strap in the pack renders as flat grey paint.
//
// Nothing in this file references a URP or HDRP type, so it compiles in a
// plain built-in project with neither package installed. The pipeline
// calls that have no built-in equivalent - re-deriving the material
// keywords after a shader swap - are made through reflection.
//
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LowPolyCity
{
    [InitializeOnLoad]
    public static class LPC_Pipeline
    {
        const string Root = "Assets/LowPolyCityFree";
        const string MatDir = Root + "/Materials";
        const string MetalSmoothPath = Root + "/Textures/LPC_Palette_MetalSmooth.png";

        public enum Pipe { BuiltIn, URP, HDRP, Unknown }

        // Shaders this script itself puts the pack's materials on. A
        // material found on one of these that is NOT the active pipeline's
        // is stale and gets converted; a material the buyer has moved to a
        // shader of their own is left alone.
        static readonly HashSet<string> OurShaders = new HashSet<string>
        {
            "Standard", "Universal Render Pipeline/Lit", "HDRP/Lit",
            "Hidden/InternalErrorShader",
        };

        static LPC_Pipeline()
        {
            // after the import has settled
            EditorApplication.delayCall += AutoRun;
        }

        // Also after the MATERIALS are (re)imported without a script
        // reload - updating the pack in a URP project brings the Standard
        // .mat files back and no domain reload follows.
        class Watch : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted,
                                               string[] moved, string[] movedFrom)
            {
                foreach (var p in imported)
                    if (p.StartsWith(MatDir + "/"))
                    {
                        EditorApplication.delayCall -= AutoRun;
                        EditorApplication.delayCall += AutoRun;
                        return;
                    }
            }
        }

        // STATE-BASED, NOT A "DONE" FLAG. The interiors pack remembered
        // "already converted" in EditorPrefs, which are per MACHINE, not
        // per project: the first URP project a buyer ever imported it into
        // converted, and every later URP project stayed magenta. Asking the
        // materials themselves whether they are on the right shader is
        // correct in every project, after every re-import, and idempotent.
        static void AutoRun()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                return;                       // pack not imported (yet)
            var pipe = Detect();
            if (pipe == Pipe.Unknown)
                return;
            string target = TargetShader(pipe);
            bool stale = false;
            foreach (var m in Materials())
            {
                string sh = m.shader == null ? "" : m.shader.name;
                if (m.shader == null
                    || (OurShaders.Contains(sh) && sh != target))
                { stale = true; break; }
            }
            if (!stale)
                return;
            Debug.Log("[LPC] " + pipe + " detected - converting the pack's "
                      + "materials. Re-run from Tools > Low Poly City.");
            Convert();
        }

        public static Pipe Detect()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
                return Pipe.BuiltIn;
            string n = rp.GetType().FullName ?? "";
            if (n.Contains("Universal"))
                return Pipe.URP;
            if (n.Contains("HighDefinition") || n.Contains("HDRenderPipeline"))
                return Pipe.HDRP;
            return Pipe.Unknown;
        }

        static string TargetShader(Pipe pipe)
        {
            return pipe == Pipe.URP ? "Universal Render Pipeline/Lit"
                 : pipe == Pipe.HDRP ? "HDRP/Lit" : "Standard";
        }

        static List<Material> Materials()
        {
            var list = new List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material",
                                                          new[] { MatDir }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (m != null) list.Add(m);
            }
            return list;
        }

        [MenuItem("Tools/Low Poly City/Set Up Materials For Current "
                  + "Pipeline", false, 1)]
        public static void Convert()
        {
            var pipe = Detect();
            string shaderName = TargetShader(pipe);
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[LPC] " + pipe + " is active but the shader \""
                               + shaderName + "\" is not installed.");
                return;
            }

            var mats = Materials();
            // Everything is READ before anything is written: the shader
            // swap drops whatever the new shader does not declare, and on
            // HDRP (below) the first save is followed by HDRP rewriting
            // values of its own - the second pass must not read those back.
            var states = new List<State>();
            foreach (var m in mats) states.Add(Capture(m));

            // THE HDRP MIGRATION TRAP. HDRP stamps every material it owns
            // with a version, and a material that EXISTED before it was
            // put on HDRP/Lit - which is every material converted in place,
            // as here - is stamped version 0 on its next import, and all of
            // HDRP's upgrade steps are replayed on it
            // (MaterialPostProcessor.k_Migrations). Two of them destroy
            // this pack's values: MetallicRemapping copies _Metallic (0)
            // into _MetallicRemapMax, so the mask map's metal is remapped
            // to 0..0 and every iron strap renders as paint; and
            // FixIncorrectEmissiveColorSpace rebuilds _EmissiveColor from an
            // LDR colour this script never set. Measured on the first HDRP
            // run: _MetallicRemapMax came out 0.
            //
            // A material HDRP CREATES is stamped with the latest version and
            // skips all that. So: write, save, make the import (and the
            // migrations, and the stamp) happen NOW, then write the same
            // captured values again. The stamp is up to date after that and
            // nothing replays.
            int passes = pipe == Pipe.HDRP ? 2 : 1;
            for (int pass = 0; pass < passes; pass++)
            {
                for (int i = 0; i < mats.Count; i++)
                {
                    Apply(mats[i], shader, pipe, states[i]);
                    EditorUtility.SetDirty(mats[i]);
                }
                AssetDatabase.SaveAssets();
                foreach (var m in mats)
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m),
                                              ImportAssetOptions.ForceSynchronousImport);
            }

            int drift = 0;
            for (int i = 0; i < mats.Count; i++)
                drift += Check(mats[i], pipe, states[i]);
            Debug.Log("[LPC] " + mats.Count + " materials on " + shaderName
                      + " for " + pipe + "."
                      + (drift > 0 ? " " + drift + " value(s) did not stick - see warnings." : ""));
        }

        struct State
        {
            public bool glass, emis, main;
            public Texture tex, metal;
            public Color col, emc;
        }

        // The three materials are named by what they do, which is the only
        // thing this needs to know: the glass one is transparent, the
        // emissive one glows, and the main one carries the metal atlas.
        static State Capture(Material m)
        {
            var s = new State();
            s.glass = m.name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0;
            s.emis = m.name.IndexOf("Emissive", StringComparison.OrdinalIgnoreCase) >= 0;
            s.main = !s.glass && !s.emis;
            s.tex = FirstTex(m, "_MainTex", "_BaseMap", "_BaseColorMap");
            s.col = m.HasProperty("_Color") ? m.GetColor("_Color")
                  : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                  : Color.white;
            // HDRP/Lit declares BOTH: _EmissiveColor is the real one,
            // _EmissionColor a lightmapper leftover it keeps at white. Read
            // the real one first so a re-run on HDRP keeps the glow.
            s.emc = m.HasProperty("_EmissiveColor") ? m.GetColor("_EmissiveColor")
                  : m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor")
                  : Color.black;
            if (s.main)
            {
                s.metal = FirstTex(m, "_MetallicGlossMap", "_MaskMap");
                if (s.metal == null)
                    s.metal = AssetDatabase.LoadAssetAtPath<Texture2D>(MetalSmoothPath);
            }
            return s;
        }

        // Did what Apply wrote survive the save and re-import?
        static int Check(Material m, Pipe pipe, State s)
        {
            int bad = 0;
            if (s.main && s.metal != null && pipe == Pipe.HDRP
                && m.HasProperty("_MetallicRemapMax") && m.GetFloat("_MetallicRemapMax") < 0.999f)
            {
                bad++;
                Debug.LogWarning("[LPC] " + m.name + "._MetallicRemapMax = "
                                 + m.GetFloat("_MetallicRemapMax") + " - metal will render as paint.");
            }
            // RGB only: HDRP's recompute scales the whole Color, alpha
            // included, and emission never reads alpha.
            var got = m.HasProperty("_EmissiveColor") ? m.GetColor("_EmissiveColor") : Color.black;
            if (s.emis && m.HasProperty("_EmissiveColor")
                && new Vector3(got.r - s.emc.r, got.g - s.emc.g, got.b - s.emc.b).magnitude > 1e-3f)
            {
                bad++;
                Debug.LogWarning("[LPC] " + m.name + "._EmissiveColor = "
                                 + m.GetColor("_EmissiveColor") + ", wanted " + s.emc);
            }
            return bad;
        }

        static void Apply(Material m, Shader shader, Pipe pipe, State st)
        {
            bool glass = st.glass, emis = st.emis, main = st.main;
            Texture tex = st.tex, metal = st.metal;
            Color col = st.col, emc = st.emc;

            m.shader = shader;

            SetTex(m, tex, "_BaseMap", "_BaseColorMap", "_MainTex");
            SetCol(m, col, "_BaseColor", "_Color");
            SetF(m, 0f, "_Metallic");
            SetF(m, glass ? 0.88f : 0.18f, "_Smoothness", "_Glossiness");

            if (glass)
            {
                // URP: _Surface 1 = transparent, _Blend 0 = alpha.
                // HDRP: _SurfaceType 1, _BlendMode 0. Setting both is
                // harmless - SetF only writes properties the shader has.
                SetF(m, 1f, "_Surface", "_SurfaceType");
                SetF(m, 0f, "_Blend", "_BlendMode");
                SetF(m, 0f, "_ZWrite");
                SetF(m, 0f, "_AlphaCutoffEnable");
                SetF(m, (float)BlendMode.SrcAlpha, "_SrcBlend");
                SetF(m, (float)BlendMode.OneMinusSrcAlpha, "_DstBlend");
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                SetF(m, 0f, "_Surface", "_SurfaceType");
                SetF(m, 1f, "_ZWrite");
                m.SetOverrideTag("RenderType", "Opaque");
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = -1;
            }

            if (emis)
            {
                m.EnableKeyword("_EMISSION");
                SetCol(m, emc, "_EmissionColor");
                SetTex(m, tex, "_EmissiveColorMap");
                // HDRP calls it _EmissiveColor, and with _UseEmissiveIntensity
                // on, its ResetMaterialKeywords (LitAPI) RECOMPUTES it as
                // linear(_EmissiveColorLDR) * _EmissiveIntensity. The
                // interiors converter set the flag and intensity 1 but never
                // the LDR colour (default black), so HDRP zeroed the glow -
                // measured: _EmissiveColor came out 0,0,0. So the HDR colour
                // is written the way HDRP's own UI stores it: an sRGB LDR
                // colour and an intensity that multiply back to it exactly.
                float scale = Mathf.Max(emc.r, Mathf.Max(emc.g, emc.b));
                if (m.HasProperty("_EmissiveColorLDR") && scale > 0f)
                {
                    var lin = emc / scale;
                    SetCol(m, new Color(Mathf.LinearToGammaSpace(lin.r),
                                        Mathf.LinearToGammaSpace(lin.g),
                                        Mathf.LinearToGammaSpace(lin.b), 1f),
                           "_EmissiveColorLDR");
                    SetF(m, 1f, "_UseEmissiveIntensity");
                    SetF(m, scale, "_EmissiveIntensity");
                }
                SetCol(m, emc, "_EmissiveColor");
                m.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            if (main)
                ApplyMetalSmooth(m, metal, pipe, false);

            // Let the pipeline re-derive its own keywords and passes from
            // the properties just written...
            if (pipe == Pipe.URP)
                UrpValidate(m);
            if (pipe == Pipe.HDRP)
                HdrpValidate(m);

            // ...then assert the metal-map keyword once more, so a pipeline
            // version whose validation does not know about it cannot
            // quietly turn it back off.
            if (main)
                ApplyMetalSmooth(m, metal, pipe, true);
        }

        static void ApplyMetalSmooth(Material m, Texture metal, Pipe pipe,
                                     bool keywordsOnly)
        {
            if (metal == null)
            {
                if (!keywordsOnly)
                    Debug.LogWarning("[LPC] " + m.name + ": " + MetalSmoothPath
                                     + " not found - metal will render as paint.");
                return;
            }
            if (!keywordsOnly)
            {
                // Built-in Standard and URP Lit: R metallic, A smoothness
                SetTex(m, metal, "_MetallicGlossMap");
                SetF(m, 0f, "_SmoothnessTextureChannel");   // metallic alpha
                SetF(m, 1f, "_GlossMapScale");              // Standard: A * this
                SetF(m, 1f, "_WorkflowMode");               // URP: 1 = metallic
                // URP multiplies the map's alpha by _Smoothness; Standard
                // ignores _Glossiness once the map is on and HDRP ignores
                // _Smoothness once the mask map is on. 1 is the only value
                // that leaves the authored smoothness exactly as authored.
                SetF(m, 1f, "_Smoothness");

                // HDRP Lit: the same texture, read as a mask map
                SetTex(m, metal, "_MaskMap");
                SetF(m, 0f, "_MetallicRemapMin");
                SetF(m, 1f, "_MetallicRemapMax");
                SetF(m, 0f, "_SmoothnessRemapMin");
                SetF(m, 1f, "_SmoothnessRemapMax");
                SetF(m, 0f, "_AORemapMin");
                SetF(m, 1f, "_AORemapMax");
            }
            Kw(m, "_METALLICGLOSSMAP", pipe == Pipe.BuiltIn);    // Standard
            Kw(m, "_METALLICSPECGLOSSMAP", pipe == Pipe.URP);     // URP Lit
            Kw(m, "_MASKMAP", pipe == Pipe.HDRP);                 // HDRP Lit
            Kw(m, "_SPECULAR_SETUP", false);                      // URP: metallic workflow
            Kw(m, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", false); // not albedo alpha
        }

        // A keyword the shader does not declare is not an error to
        // enable - Unity just files it under "invalid" and nothing
        // renders differently. So check, and SAY so: a pipeline update
        // that renames one of these shows up in the Console instead of as
        // a pack that has quietly gone back to painted iron.
        static void Kw(Material m, string name, bool on)
        {
            var k = new LocalKeyword(m.shader, name);
            if (k.isValid)
            {
                m.SetKeyword(k, on);
                return;
            }
            m.DisableKeyword(name);
            if (on)
                Debug.LogWarning("[LPC] " + m.shader.name + " has no keyword "
                                 + name + " - " + m.name + " will not use its "
                                 + "metal/smoothness map.");
        }

        static Texture FirstTex(Material m, params string[] names)
        {
            foreach (var n in names)
                if (m.HasProperty(n) && m.GetTexture(n) != null)
                    return m.GetTexture(n);
            return null;
        }

        static void SetTex(Material m, Texture t, params string[] names)
        {
            if (t == null) return;
            foreach (var n in names)
                if (m.HasProperty(n)) m.SetTexture(n, t);
        }

        static void SetCol(Material m, Color c, params string[] names)
        {
            foreach (var n in names)
                if (m.HasProperty(n)) m.SetColor(n, c);
        }

        static void SetF(Material m, float v, params string[] names)
        {
            foreach (var n in names)
                if (m.HasProperty(n)) m.SetFloat(n, v);
        }

        // URP derives its keywords (surface type, blending, the metal
        // map's _METALLICSPECGLOSSMAP) from the material's properties in
        // its ShaderGUI's ValidateMaterial - which the editor calls when a
        // material is shown in the Inspector, not when a script swaps the
        // shader. Called here so the material is right before anyone
        // opens it. Reflection: this file compiles with no URP installed.
        static void UrpValidate(Material m)
        {
            var t = Type.GetType(
                "UnityEditor.Rendering.Universal.ShaderGUI.LitShader, "
                + "Unity.RenderPipelines.Universal.Editor");
            if (t == null) return;
            try
            {
                var gui = Activator.CreateInstance(t) as ShaderGUI;
                if (gui != null) gui.ValidateMaterial(m);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LPC] URP validate " + m.name + ": " + e.Message);
            }
        }

        // HDRP keeps a pile of keywords and passes in sync with the
        // material's properties and will render it wrong until they are
        // reset. The call lives in the HDRP package, so it is made through
        // reflection - this file has to compile with no HDRP installed.
        static void HdrpValidate(Material m)
        {
            var t = Type.GetType(
                "UnityEditor.Rendering.HighDefinition.HDShaderUtils, "
                + "Unity.RenderPipelines.HighDefinition.Editor");
            if (t == null) return;
            var mi = t.GetMethod("ResetMaterialKeywords",
                                 BindingFlags.Public | BindingFlags.Static,
                                 null, new[] { typeof(Material) }, null);
            if (mi == null) return;
            try { mi.Invoke(null, new object[] { m }); }
            catch (Exception e)
            {
                Debug.LogWarning("[LPC] HDRP validate " + m.name + ": " + e.Message);
            }
        }
    }
}
