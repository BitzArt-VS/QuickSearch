namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Restricts what input is accepted by a <see cref="GuiTextInput"/>:
/// <list type="bullet">
///   <item><see cref="Text"/> — any character is accepted.</item>
///   <item><see cref="Integer"/> — only optional leading minus + decimal digits.
///   Intermediate states like the empty string and a lone <c>"-"</c> are also accepted
///   so the user can edit through them; full validation runs on every candidate before
///   the internal text is mutated.</item>
///   <item><see cref="Decimal"/> — like <see cref="Integer"/> but additionally allows a
///   single <c>'.'</c> separator (and a trailing dot during editing, e.g. <c>"1."</c>).</item>
/// </list>
/// </summary>
public enum GuiTextInputMode
{
    Text,
    Integer,
    Decimal,
}
