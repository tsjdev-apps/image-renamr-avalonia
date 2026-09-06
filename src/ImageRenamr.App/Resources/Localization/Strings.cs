using System.Globalization;
using System.Resources;

namespace ImageRenamr.App.Resources.Localization;

#pragma warning disable CA1707 // Resource accessor names intentionally mirror semantic .resx keys.

/// <summary>
/// Provides strongly named access to localized application resources.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("ImageRenamr.App.Resources.Localization.Strings", typeof(Strings).Assembly);

    public static string ThemeLocale => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de"
        ? "de-DE"
        : "en-US";

    public static string App_Title => Get();
    public static string BatchSetup_Title => Get();
    public static string BatchSetup_Description => Get();
    public static string InputFolder_Label => Get();
    public static string InputFolder_Placeholder => Get();
    public static string InputFolder_PickerTitle => Get();
    public static string OutputFolder_Label => Get();
    public static string OutputFolder_Placeholder => Get();
    public static string OutputFolder_PickerTitle => Get();
    public static string Browse_ToolTip => Get();
    public static string Prefix_Label => Get();
    public static string Prefix_Placeholder => Get();
    public static string Overwrite_Label => Get();
    public static string Overwrite_Description => Get();
    public static string RenameImages_Button => Get();
    public static string RenamingImages_Button => Get();
    public static string Progress_Title => Get();
    public static string Progress_OverallTitle => Get();
    public static string Progress_HistoryTitle => Get();
    public static string Progress_NoFiles => Get();
    public static string Status_Ready => Get();
    public static string Status_Preparing => Get();
    public static string Status_Scanning => Get();
    public static string Status_NoImages => Get();
    public static string Status_Completed => Get();
    public static string Summary_Validating => Get();
    public static string Summary_NoImages => Get();
    public static string Summary_BatchFailed => Get();
    public static string RenameStatus_Renamed => Get();
    public static string RenameStatus_Overwritten => Get();
    public static string RenameStatus_Skipped => Get();
    public static string RenameStatus_Failed => Get();
    public static string RenameStatus_Completed => Get();
    public static string RenameHistory_CompletedTitle => Get();
    public static string RenameHistory_NoFilesTitle => Get();
    public static string RenameHistory_FailedTitle => Get();
    public static string RenameError_TargetExists => Get();
    public static string RenameError_TargetExistsShort => Get();
    public static string RenameError_FileSystem => Get();
    public static string RenameError_AccessDenied => Get();
    public static string Validation_ReviewInput => Get();
    public static string Validation_FoldersRequired => Get();
    public static string Validation_FoldersMustDiffer => Get();
    public static string Validation_PrefixRequired => Get();
    public static string Validation_PrefixInvalid => Get();
    public static string Validation_InputMissing => Get();
    public static string Validation_FileSystem => Get();
    public static string Validation_AccessDenied => Get();

    public static string FormatProgress(int processedCount, int totalCount)
    {
        string key = totalCount == 1
            ? "Progress_ProcessedFile"
            : "Progress_ProcessedFiles";

        return Format(key, processedCount, totalCount);
    }

    public static string FormatSummary(int copiedCount, int skippedCount) =>
        Format("Summary_Completed", copiedCount, skippedCount);

    public static string FormatNewName(string newFileName) =>
        Format("RenameHistory_NewName", newFileName);

    public static string FormatTargetExists(string newFileName) =>
        Format("RenameError_TargetExists", newFileName);

    private static string Get([System.Runtime.CompilerServices.CallerMemberName] string key = "") =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private static string Format(string key, params object[] arguments) =>
        string.Format(
            CultureInfo.CurrentCulture,
            ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key,
            arguments);
}

#pragma warning restore CA1707
