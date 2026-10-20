using System;

namespace ElectricGrid.Core.DataStructures
{
    /// <summary>A suggestion returned by a prefix search.</summary>
    public readonly struct TrieMatch
    {
        /// <summary>Creates a match.</summary>
        public TrieMatch(string text, string payload)
        {
            Text = text;
            Payload = payload;
        }

        /// <summary>Original (display) text that was inserted (the substation name).</summary>
        public string Text { get; }

        /// <summary>Value associated with the text (the substation code).</summary>
        public string Payload { get; }
    }

    /// <summary>
    /// Prefix tree (trie). PENDING IMPLEMENTATION - skeleton with the contract the rest of the
    /// system expects.
    /// <para>
    /// ROLE IN THE SYSTEM: suggest substation names while the operator types. Each terminal node
    /// keeps the substation code(s) as payload so the suggestion can be resolved into the full
    /// record through the hash table.
    /// </para>
    /// </summary>
    /// <remarks>
    /// TODO [TRIE - assigned teammate]: implement every method below with your own nodes
    /// (no Dictionary / List / SortedSet). Recommended behaviour:
    /// case- and accent-insensitive keys, several entries may share the same name (different codes),
    /// results in alphabetical order, deletion that prunes empty branches.
    /// After implementing it, wire it into the service: search the project for "TODO [TRIE".
    /// </remarks>
    public sealed class Trie
    {
        /// <summary>Number of stored entries.</summary>
        public int Count => throw new NotImplementedException("Trie is pending implementation.");

        /// <summary>Number of nodes in the tree (including the root).</summary>
        public int NodeCount => throw new NotImplementedException("Trie is pending implementation.");

        /// <summary>Inserts a (text, payload) pair. Returns <c>false</c> if the same pair already exists.</summary>
        public bool Insert(string text, string payload)
        {
            // TODO [TRIE]: insert one node per character of the normalized text; store the payload in the terminal node.
            throw new NotImplementedException("Trie is pending implementation.");
        }

        /// <summary>Checks whether a text (any payload) is stored.</summary>
        public bool Contains(string text)
        {
            // TODO [TRIE]
            throw new NotImplementedException("Trie is pending implementation.");
        }

        /// <summary>Removes a (text, payload) pair and prunes empty branches. Returns <c>true</c> if it existed.</summary>
        public bool Remove(string text, string payload)
        {
            // TODO [TRIE]
            throw new NotImplementedException("Trie is pending implementation.");
        }

        /// <summary>Prefix search used by the autocomplete feature (alphabetical, at most <paramref name="maxResults"/>).</summary>
        public TrieMatch[] SearchByPrefix(string prefix, int maxResults)
        {
            // TODO [TRIE]: walk to the prefix node, then collect the entries of its subtree.
            throw new NotImplementedException("Trie is pending implementation.");
        }

        /// <summary>Traverses the whole trie and returns every entry.</summary>
        public TrieMatch[] GetAll()
        {
            // TODO [TRIE]
            throw new NotImplementedException("Trie is pending implementation.");
        }

        /// <summary>Builds a printable listing of the stored entries and the tree size (used by menu option 21).</summary>
        public string Describe()
        {
            // TODO [TRIE]
            throw new NotImplementedException("Trie is pending implementation.");
        }
    }
}
