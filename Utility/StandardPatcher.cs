using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Util = Completionist_GUI_Patcher.Utility.Utility;

namespace Completionist_GUI_Patcher.Utility
{
    internal class StandardPatcher
    {
        public static int Patch(Patcher instance, string? filePath, string className)
        {
            // Define extraction directory and script paths.
            string curWorkingDir = Directory.GetCurrentDirectory();
            string extractionDir = Path.Combine(curWorkingDir, "extracted");
            string actionScriptFile = Path.Combine(extractionDir, "scripts", "__Packages", $"{className}.as");
            string scriptName = $@"\__Packages\{className}";
            string ffdecPath = Path.Combine(curWorkingDir, "ffdec", "ffdec-cli.exe");
            string extractCommand = $"\"{ffdecPath}\" -export script \"{extractionDir}\" \"{filePath}\"";
            string replaceCommand = $"\"{ffdecPath}\" -replace \"{filePath}\" \"{filePath}\" \"{scriptName}\" \"{actionScriptFile}\"";
            string backupPath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                instance.UpdateLog($"Error: filePath cannot be null");
                instance.UpdateLog("Exiting...");
                return 0;
            }

            // Create backup file.
            try
            {
                backupPath = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileName(filePath) + ".completionist_backup");
                if (!File.Exists(backupPath))
                {
                    File.Copy(filePath, backupPath, overwrite: false);
                    instance.UpdateLog($"Backup created at: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                instance.UpdateLog($"Error creating backup: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return 0;
            }

            // Create extraction directory if it doesn't exist.
            if (!Directory.Exists(extractionDir))
            {
                try
                {
                    Directory.CreateDirectory(extractionDir);
                }
                catch (Exception ex)
                {
                    instance.UpdateLog($"Unable to create extraction directory: {ex.Message}");
                    File.Delete(backupPath);
                    instance.UpdateLog("Exiting...");
                    return 0;
                }
            }

            // ========== Step 1: Extract ActionScript ==========
            instance.UpdateLog($"Extracting ActionScript from {Path.GetFileName(filePath)}...");
            instance.UpdateLog($"Command: {extractCommand}");

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{extractCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo) ?? throw new Exception("Failed to start FFDec process.");

                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int extractResult = process.ExitCode;

                // Always log stdout/stderr, not just when debugging
                if (!string.IsNullOrEmpty(output))
                    instance.UpdateLog($"FFDec stdout: {output}");
                if (!string.IsNullOrEmpty(errorOutput))
                    instance.UpdateLog($"FFDec stderr: {errorOutput}");

                if (extractResult != 0)
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog($"FFDec extraction failed with exit code {extractResult}.");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);
                instance.UpdateLog($"Exception while running FFDec: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return 0;
            }

            instance.UpdateLog($"ActionScript extracted successfully.");

            // ========== Step 2: Read and modify ActionScript ==========
            string scriptContent;
            try
            {
                scriptContent = File.ReadAllText(actionScriptFile);
                if (string.IsNullOrEmpty(scriptContent))
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog($"Failed to read ActionScript file: {actionScriptFile}");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }

                // Check if already patched
                if (scriptContent.Contains("CompTag"))
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog($"{Path.GetFileName(filePath)} already has completionist support. Skipping.");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);
                instance.UpdateLog($"Error reading ActionScript file: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return 0;
            }

            try
            {
                if (string.IsNullOrEmpty(scriptContent))
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog("Script content is empty or null.");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }

                // Define the regex pattern to find the line with 'var _locX_ = a_entryObject.text;'
                string pattern = @"var\s+([_a-zA-Z0-9]+)\s*=\s*a_entryObject\.text;";
                var regex = new Regex(pattern);

                if (regex.IsMatch(scriptContent))
                {
                    var match = regex.Match(scriptContent);
                    if (match.Groups.Count > 1)
                    {
                        string variableName = match.Groups[1].Value;
                        string newLines = $"\n      var _comp_ = {variableName}.split(\"CompTag\");\n";
                        newLines += $"      {variableName} = _comp_[0].length > 0 ? _comp_[0] : {variableName};\n";

                        string additionalLines = @"
      if(a_entryObject.enabled == true && _comp_[1].length > 0)
      {
         a_entryField.textColor = parseInt(_comp_[1]);
      }
            ";

                        string targetLine = $"var {variableName} = a_entryObject.text;";
                        var (modifiedContent, success) = Util.InsertBaseLinesAfterVariable(scriptContent, targetLine, newLines, instance);
                        if (!success)
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to insert lines after variable.");
                            instance.UpdateLog("Exiting...");
                            return 0;
                        }

                        string colorInsertString = "this.formatColor(a_entryField,a_entryObject,a_state);";
                        var (finalContent, success2) = Util.InsertBaseLinesAfter(modifiedContent, colorInsertString, additionalLines, instance);
                        if (!success2)
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to insert color formatting.");
                            instance.UpdateLog("Exiting...");
                            return 0;
                        }

                        if (!Util.WriteStringToFile(actionScriptFile, finalContent, instance))
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to write modified content to file.");
                            instance.UpdateLog("Exiting...");
                            return 0;
                        }
                    }
                    else
                    {
                        File.Delete(backupPath);
                        Directory.Delete(extractionDir, true);
                        instance.UpdateLog("Failed to extract variable name from pattern match.");
                        instance.UpdateLog("Exiting...");
                        return 0;
                    }
                }
                else
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog("Pattern not found in ActionScript file.");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }

                instance.UpdateLog("ActionScript modification successful.");
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);
                instance.UpdateLog($"Error modifying ActionScript: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return 0;
            }

            // ========== Step 3: Recompile SWF ==========
            instance.UpdateLog($"Recompiling SWF with modified script...");
            instance.UpdateLog($"Command: {replaceCommand}");

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{replaceCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo) ?? throw new Exception("Failed to start FFDec process.");

                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
                int result = process.ExitCode;

                if (!string.IsNullOrEmpty(output))
                    instance.UpdateLog($"FFDec stdout: {output}");
                if (!string.IsNullOrEmpty(errorOutput))
                    instance.UpdateLog($"FFDec stderr: {errorOutput}");

                if (result != 0)
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog($"FFDec replacement failed with exit code {result}.");
                    instance.UpdateLog("Exiting...");
                    return 0;
                }

                instance.UpdateLog("SWF compiled successfully.");
                Directory.Delete(extractionDir, true);
                return 1;
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);
                instance.UpdateLog($"Exception during FFDec replacement: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return 0;
            }
        }
    }
}