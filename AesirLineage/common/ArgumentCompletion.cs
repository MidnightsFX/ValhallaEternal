using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace AesirLineage.common
{
    /// <summary>A console command that completes every argument with Tab, not only the first as the game does.</summary>
    internal interface ICompletesArguments
    {
        /// <summary>What the argument at <paramref name="index"/> can be, given the arguments typed before it. Null when anything goes.</summary>
        List<string> ArgumentOptions(int index, string[] before);
    }

    /// <summary>
    /// The game completes a command's arguments, and lists matches under the input, only for its first
    /// argument: it hands <c>tabCycle</c> and <c>updateSearch</c> the first argument's word and options
    /// however far along the line is. For an <see cref="ICompletesArguments"/> command they get the word
    /// being typed and the options for its place instead.
    /// </summary>
    /// <remarks>
    /// Jotunn builds each command's options fetcher from the command's own <c>CommandOptionList</c>, so
    /// the fetcher's target is the Jotunn command. These options are worked out on every key, so they never
    /// go stale the way the game's cached ones do.
    /// </remarks>
    [HarmonyPatch(typeof(Terminal))]
    internal static class ArgumentCompletion
    {
        [HarmonyPatch(nameof(Terminal.tabCycle)), HarmonyPrefix]
        private static void TabCycle(Terminal __instance, ref string word, ref List<string> options, bool usePrefix)
        {
            Redirect(__instance, ref word, ref options, usePrefix);
        }

        [HarmonyPatch(nameof(Terminal.updateSearch)), HarmonyPrefix]
        private static void UpdateSearch(Terminal __instance, ref string word, ref List<string> options, bool usePrefix)
        {
            Redirect(__instance, ref word, ref options, usePrefix);
        }

        // usePrefix is set only while completing the command's name, which the game does well enough.
        private static void Redirect(Terminal terminal, ref string word, ref List<string> options, bool usePrefix)
        {
            if (usePrefix || terminal.m_input == null) { return; }
            string[] words = terminal.m_input.text.Split(' ');
            if (words.Length < 2) { return; }
            string name = terminal.m_tabPrefix == '\0' || words[0].Length == 0 ? words[0] : words[0].Substring(1);
            if (!Terminal.commands.TryGetValue(name.ToLower(), out Terminal.ConsoleCommand command)) { return; }
            if (!(command.m_tabOptionsFetcher?.Target is ICompletesArguments completes)) { return; }

            int index = words.Length - 2;
            word = words[words.Length - 1];
            options = completes.ArgumentOptions(index, words.Skip(1).Take(index).ToArray()) ?? new List<string>();
        }
    }
}
