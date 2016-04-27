using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kontur.Intern {
    public enum Colors
    {
        Red = 1,
        Blue,
        Green,
        Yellow,
        White,
    };
    public enum Ranks
    {
        One = 1,
        Two,
        Three,
        Four,
        Five,
    };
    public class Card
    {
        public Colors Color { get; private set; }
        public Ranks Rank { get; private set; }
        public SortedSet<Colors> AllowedColors { get; private set; }
        public SortedSet<Ranks> AllowedRanks { get; private set; }

        public Card(Char color, Int32 rankIndex)
        {
            switch (color)
            {
                case 'R':
                    Color = Colors.Red;
                    break;
                case 'B':
                    Color = Colors.Blue;
                    break;
                case 'G':
                    Color = Colors.Green;
                    break;
                case 'W':
                    Color = Colors.White;
                    break;
                case 'Y':
                    Color = Colors.Yellow;
                    break;
                default:
                    throw new Exception("Unknown letter for color");
            }

            Rank = (Ranks)Enum.GetValues(typeof(Ranks)).GetValue(rankIndex - 1);
            AllowedRanks = new SortedSet<Ranks>(Enum.GetValues(typeof(Ranks)) as IEnumerable<Ranks>);
            AllowedColors = new SortedSet<Colors>(Enum.GetValues(typeof(Colors)) as IEnumerable<Colors>);
        }

        public void MarkColor(Colors color)
        {
            if (Color == color)
            {
                AllowedColors.Clear();
                AllowedColors.Add(color);
            }
            else
            {
                AllowedColors.Remove(color);
            }
        }
        public void MarkRank(Ranks rank)
        {
            if (Rank == rank)
            {
                AllowedRanks.Clear();
                AllowedRanks.Add(rank);
            }
            else
            {
                AllowedRanks.Remove(rank);
            }
        }
    }
    public class Deck
    {
        private Queue<Card> deck;
        public Deck(Match match)
        {
            deck = new Queue<Card>();
            String[] cards = match.Groups["deck"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < cards.Length; ++i)
            {
                deck.Enqueue(new Card(cards[i][0], Int32.Parse(cards[i][1].ToString())));
            }
        }

        public Card TakeCard()
        {
            return deck.Dequeue();
        }

        public List<Card> TakeCard(Int32 count)
        {
            List<Card> cards = new List<Card>();

            while (cards.Count < count)
            {
                cards.Add(deck.Dequeue());
            }

            return cards;
        }

        public Boolean IsEmpty
        {
            get { return deck.Count == 0; }
        }
    }
    public class Table
    {
        List<Card> table;
        public Table()
        {
            table = new List<Card>(Enum.GetValues(typeof(Colors)).Length);
        }
        public void PutCard(Card card)
        {
            table.Add(card);
        }
        public Boolean IsFull
        {
            get { return table.Count == Enum.GetValues(typeof(Colors)).GetLength(0) * Enum.GetValues(typeof(Ranks)).GetLength(0); }
        }
        public Int32 CardsCount
        {
            get { return table.Count; }
        }
        public List<Card> GetCardsWithColor(Colors color)
        {
            return table.Where(obj => obj.Color == color).ToList();
        }
        public List<Colors> GetPossibleColors(Ranks rank)
        {
            Int32 rankIndex = (Int32)rank;
            List<Colors> allowColors = new List<Colors>();
            List<Colors> colors = new List<Colors>(Enum.GetValues(typeof(Colors)) as IEnumerable<Colors>);
            foreach (Colors color in colors)
            {
                if (table.Count(obj => obj.Color == color) == rankIndex - 1)
                    allowColors.Add(color);
            }
            return allowColors;
        }
    }
    public class Player
    {
        public Int32 Cards { get; private set; }
        public Int32 Risk { get; private set; }
        private List<Card> hand;
        public Player(List<Card> startHand)
        {
            Cards = Risk = 0;
            hand = startHand;
        }
        public Card LayoutCard(Int32 index)
        {
            Card card = hand[index];
            hand.RemoveAt(index);
            return card;
        }
        public void TakeCard(Card card)
        {
            hand.Add(card);
        }
        public List<int> GetCardsIndexesWithRank(Ranks rank)
        {
            return hand.Select((obj, index) => new { Item = obj, Index = index }).Where(obj => obj.Item.Rank == rank).Select(obj => obj.Index).ToList();
        }
        public List<int> GetCardsIndexesWithColor(Colors color)
        {
            return hand.Select((obj, index) => new { Item = obj, Index = index }).Where(obj => obj.Item.Color == color).Select(obj => obj.Index).ToList();
        }
        public void MarkCardColor(Colors color)
        {
            hand.ForEach(card => card.MarkColor(color));
        }
        public void MarkCardRank(Ranks rank)
        {
            hand.ForEach(card => card.MarkRank(rank));
        }
    }
    public class Game
    {
        private static Regex regexPlayCard = new Regex(@"Play card (?<index>[0-4])", RegexOptions.Compiled);
        private static Regex regexDropCard = new Regex(@"Drop card (?<index>[0-4])", RegexOptions.Compiled);
        private static Regex regexTellColor = new Regex(@"Tell color (?<color>Red|Blue|Green|White|Yellow) for cards (?<indexes>[0-4]\s*)+", RegexOptions.Compiled);
        private static Regex regexTellRank = new Regex(@"Tell rank (?<rank>[1-5]) for cards (?<indexes>[0-4]\s*)+", RegexOptions.Compiled);
        private Table table;
        private Deck deck;
        private Player activePlayer, passivePlayer; 
        private Int32 Turn, Risk;
        private static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
        public Game(Match match)
        {
            deck = new Deck(match);
            table = new Table();
            activePlayer = new Player(deck.TakeCard(5));
            passivePlayer = new Player(deck.TakeCard(5));
            Turn = Risk = 0;
        }
        public Boolean IsDangerousTurn(Card card)
        {
            if (card.AllowedColors.Count == 1 && card.AllowedRanks.Count == 1)
                return false;
            if (card.AllowedRanks.Count != 1)
               return true;
            if (table.GetPossibleColors(card.Rank).Intersect(card.AllowedColors).SequenceEqual(card.AllowedColors))
                return false;
            return true;
        }
        public Boolean PlayCard(Match match)
        {
            Int32 index = Int32.Parse(match.Groups["index"].Value);
            Card card = activePlayer.LayoutCard(index);
            activePlayer.TakeCard(deck.TakeCard());
            List<Card> cardsWithColor = table.GetCardsWithColor(card.Color);
            if (cardsWithColor.Count + 1 == (Int32)card.Rank)
            {
                if (IsDangerousTurn(card))
                    ++Risk;
                table.PutCard(card);
                return true;
            }
            return false;
        }
        public Boolean DropCard(Match match)
        {
            Int32 index = Int32.Parse(match.Groups["index"].Value);
            Card card = activePlayer.LayoutCard(index);
            activePlayer.TakeCard(deck.TakeCard());
            return true;
        }
        public Boolean TellColor(Match match)
        {
            Colors color = GetColorFromString(match.Groups["color"].Value);
            List<Int32> indexes = new List<Int32>();
            foreach (Capture capture in match.Groups["indexes"].Captures)
            {
                Int32 index = Int32.Parse(capture.Value.Trim());
                indexes.Add(index);
            }
            List<Int32> cardWithColor = passivePlayer.GetCardsIndexesWithColor(color);
            if (cardWithColor.SequenceEqual(indexes))
            {
                passivePlayer.MarkCardColor(color);
                return true;
            }
            return false;
        }
        public Boolean TellRank(Match match)
        {
            Int32 rankIndex = Int32.Parse(match.Groups["rank"].Value);
            Ranks rank = (Ranks)Enum.GetValues(typeof(Ranks)).GetValue(rankIndex - 1);
            List<Int32> indexes = new List<Int32>();
            foreach (Capture capture in match.Groups["indexes"].Captures)
            {
                Int32 index = Int32.Parse(capture.Value.Trim());
                indexes.Add(index);
            }
            List<Int32> cardWithRank = passivePlayer.GetCardsIndexesWithRank(rank);
            if (cardWithRank.SequenceEqual(indexes))
            {
                passivePlayer.MarkCardRank(rank);
                return true;
            }
            return false;
        }
        public Boolean ExecuteCommand(String command)
        {
            ++Turn;
            Match match = Match.Empty;
            Boolean result = false;
            if ((match = regexPlayCard.Match(command)) != Match.Empty)
            {
                result = PlayCard(match);
            }
            else if ((match = regexDropCard.Match(command)) != Match.Empty)
            {
                result = DropCard(match);
            }
            else if ((match = regexTellColor.Match(command)) != Match.Empty)
            {
                result = TellColor(match);
            }
            else if ((match = regexTellRank.Match(command)) != Match.Empty)
            {
                result = TellRank(match);
            }
            result &= !deck.IsEmpty;
            result &= !table.IsFull;
            Swap(ref activePlayer, ref passivePlayer);
            return result;
        }
        private Colors GetColorFromString(String queryColor)
        {
            foreach (Colors color in Enum.GetValues(typeof(Colors)))
            {
                if (String.Compare(queryColor, color.ToString(), StringComparison.Ordinal) == 0)
                    return color;
            }
            return Colors.Red;
        }
        public override string ToString()
        {
            return String.Format("Turn: {0}, cards: {1}, with risk: {2}", Turn, table.CardsCount, Risk);
        }
    }
    class Program
    {
        private static Regex regexStartGame = new Regex(@"Start new game with deck(?<deck>(\s*\w\d)+)", RegexOptions.Compiled);
        static void Main(string[] args)
        {
            String command = String.Empty;
            Game game = null;
            Match match = Match.Empty;
            Boolean isFinished = true;
            while ((command = Console.ReadLine()) != null)
            {
                if ((match = regexStartGame.Match(command)) != Match.Empty)
                {
                    isFinished = false;
                    game = new Game(match);
                }
                else if (!isFinished)
                {
                    isFinished = !game.ExecuteCommand(command);
                    if (isFinished)
                    {
                        Console.WriteLine(game);
                    }
                }
            }
        }
    }
}