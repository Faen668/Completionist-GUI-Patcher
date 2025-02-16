using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Util = Completionist_GUI_Patcher.Utility.Utility;

namespace Completionist_GUI_Patcher.Utility
{
    internal class StandardPatcher
    {
        public static void Patch(Patcher instance, string? filePath, string className)
        {
            // Define extraction directory and script paths.
            string curWorkingDir = Directory.GetCurrentDirectory();
            string extractionDir = Path.Combine(curWorkingDir, "extracted");
            string actionScriptFile = Path.Combine(extractionDir, "scripts", "__Packages", $"{className}.as");
            string scriptName = $@"\__Packages\{className}";
            string ffdecPath = Path.Combine(curWorkingDir, "ffdec_22.0.2_nightly3026", "ffdec-cli.exe");
            string extractCommand = $"\"{ffdecPath}\" -export script \"{extractionDir}\" \"{filePath}\"";
            string replaceCommand = $"\"{ffdecPath}\" -replace \"{filePath}\" \"{filePath}\" \"{scriptName}\" \"{actionScriptFile}\"";
            string backupPath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                instance.UpdateLog($"Error: filePath cannot be null");
                instance.UpdateLog("Exiting...");
                return;
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
                return;
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
                    return;
                }
            }

            try
            {
                // Start the process
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

                // Capture the output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                // Check the exit code
                int extractResult = process.ExitCode;

                // Log output only if a debugger is attached
                if (Debugger.IsAttached)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        instance.UpdateLog("FFDec Output: " + output);
                    }
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        instance.UpdateLog("FFDec Error Output: " + errorOutput);
                    }
                }

                if (extractResult != 0)
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                    instance.UpdateLog($"Failed to extract ActionScript using FFDec. Error code: {extractResult}");
                    instance.UpdateLog("Exiting...");
                    return;
                }
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                instance.UpdateLog($"Error executing FFDec: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return;
            }

            instance.UpdateLog($"Actionscript extracted successfully.");

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
                    return;
                }

                // Check if the script content already has completionist support
                if (scriptContent.Contains("CompTag"))
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog($"{Path.GetFileName(filePath)} already has completionist support");
                    instance.UpdateLog("Exiting...");
                    return;
                }
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);
                instance.UpdateLog($"Error reading ActionScript file: {ex.Message}");
                instance.UpdateLog("Exiting...");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(scriptContent))
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);
                    instance.UpdateLog("Script content is empty or null.");
                    instance.UpdateLog("Exiting...");
                    return;
                }

                // Define the regex pattern to find the line with 'var _locX_ = a_entryObject.text;'
                string pattern = @"var\s+([_a-zA-Z0-9]+)\s*=\s*a_entryObject\.text;";
                var regex = new Regex(pattern);

                // Search for the pattern in the script content
                if (regex.IsMatch(scriptContent))
                {
                    var match = regex.Match(scriptContent);

                    if (match.Groups.Count > 1)
                    {
                        string variableName = match.Groups[1].Value; // Extracts _locX_ or similar variable name
                        string newLines = $"\n      var _comp_ = {variableName}.split(\"CompTag\");\n";
                        newLines += $"      {variableName} = _comp_[0].length > 0 ? _comp_[0] : {variableName};\n";

                        // Insert specific lines after a given line
                        string additionalLines = @"
      if(a_entryObject.enabled == true && _comp_[1].length > 0)
      {
         a_entryField.textColor = parseInt(_comp_[1]);
      }
            ";

                        // Modify the script content
                        string targetLine = $"var {variableName} = a_entryObject.text;";
                        var (modifiedContent, success) = Util.InsertBaseLinesAfterVariable(scriptContent, targetLine, newLines, instance);
                        if (!success)
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to insert lines after variable.");
                            instance.UpdateLog("Exiting...");
                            return;
                        }

                        string colorInsertString = "this.formatColor(a_entryField,a_entryObject,a_state);";
                        var (finalContent, success2) = Util.InsertBaseLinesAfter(modifiedContent, colorInsertString, additionalLines, instance);
                        if (!success2)
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to insert color formatting.");
                            instance.UpdateLog("Exiting...");
                            return;
                        }

                        // Now, write the modified content back
                        if (!Util.WriteStringToFile(actionScriptFile, finalContent, instance))
                        {
                            File.Delete(backupPath);
                            Directory.Delete(extractionDir, true);
                            instance.UpdateLog("Failed to write modified content to file.");
                            instance.UpdateLog("Exiting...");
                            return;
                        }
                    }
                    else
                    {
                        File.Delete(backupPath);
                        Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                        instance.UpdateLog("Failed to extract variable name from pattern match.");
                        instance.UpdateLog("Exiting...");
                        return;
                    }
                }
                else
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                    instance.UpdateLog("Pattern not found in ActionScript file.");
                    instance.UpdateLog("Exiting...");
                    return;
                }

                // Finished
                instance.UpdateLog("ActionScript modification successful...");
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                instance.UpdateLog($"Error modifying ActionScript: {ex.Message}");
                instance.UpdateLog("Exiting...");
            }

            // Step 4: Recompile the swf file.
            try
            {
                // Start the process
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

                // Capture the output and error streams
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                // Check the exit code
                int result = process.ExitCode;

                // Log output only if a debugger is attached
                if (Debugger.IsAttached)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        instance.UpdateLog("FFDec Output: " + output);
                    }
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        instance.UpdateLog("FFDec Error Output: " + errorOutput);
                    }
                }

                if (result != 0)
                {
                    File.Delete(backupPath);
                    Directory.Delete(extractionDir, true);  // Use true to delete non-empty directories
                    instance.UpdateLog($"Failed to replace ActionScript using FFDec. Error code: {result}");
                    instance.UpdateLog("Exiting...");
                    return;
                }

                instance.UpdateLog("SWF compiled successfully...");
                Directory.Delete(extractionDir, true);  // Clean up extraction directory
            }
            catch (Exception ex)
            {
                File.Delete(backupPath);
                Directory.Delete(extractionDir, true);  // Clean up extraction directory
                instance.UpdateLog($"Error executing FFDec replace: {ex.Message}");
                instance.UpdateLog("Exiting...");
            }
        }
    }
}
