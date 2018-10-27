using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player4_older : Player
    {
        public Player4_older(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
        }

        int opponentDiscards = 0;
        int timesRaisedBet1 = 0;
        int timesRaisedBet2 = 0;
        string betAmtText = "10";

        /* Variables for bluffing */
        int bluffCounter = 0;
        bool alreadyCountedBluff = false;
        bool bluffing = false;
        int timeToBluff = 10; // Don't bluff for the first 10 hands, in case an opponent might be checking for bluffs then disable their counterplay
        bool disableBluffing = false; // If an opponent doesn't fall for 'stand pat' bluff then flip this so we don't lose more money

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand) // First bets after the draw
        {
            if (!alreadyCountedBluff) // Increment the bluff counter
            {
                bluffCounter++;
                alreadyCountedBluff = true;
            }

            ListTheHand(hand);

            int rank = Evaluate.RateAHand(hand, out Card highCard); // Take in the hand rank to make decisions

            string actionSelection = "";
            PlayerAction pa = null;
            do
            {
                Console.WriteLine("Select an action:\n1 - bet\n2 - raise\n3 - call\n4 - check\n5 - fold");

                //AI input for this round
                /* Decision Tree
                 * 
                 *              BET1            |           DRAW      |                 BET2
                 *             /    \                        |                      /           \
                 *          FIRST   SECOND                  RANK                FIRST           SECOND
                 *           RANK     ACTION                              CHECK THEIR DISCARD    ACTION     
                 *                    RANK                                CHECK RANK             CHECK THEIR DISCARD
                 *                                                                               CHECK RANK
                 * 
                 */


                if (actions.Count < 1) // If we go first, just start the betting
                {
                    actionSelection = Bet1Start(rank, hand);
                }
                else
                {
                    PlayerAction lastAction = actions[actions.Count - 1]; // Find out which part of betting round 1 we're in

                    if (lastAction.ActionPhase.Equals("Bet1")) // Going second in BET1
                    {
                        actionSelection = Bet1(rank, hand, lastAction);
                    }
                    else if (lastAction.ActionPhase.Equals("Bet2")) // Going second in BET2
                    {
                        actionSelection = Bet2(rank, hand, lastAction, actions);
                    }
                    else if (lastAction.ActionPhase.ToLower().Equals("draw")) // Going first in BET2
                    {
                        actionSelection = Bet2Start(rank, hand, lastAction);
                    }
                }

                int amount = 0;
                if (actionSelection[0] == '1' || actionSelection[0] == '2')
                {
                    do
                    {
                        int tempAmt = 0;
                        int.TryParse(betAmtText, out tempAmt);

                        if (tempAmt > Money) amount = 0; // Trying to bet more than we have
                        else if (tempAmt < 0) amount = 0; // Trying to bet and putting up no money
                        else amount = tempAmt;
                    } while (amount <= 0);
                }

                switch (actionSelection) // Create the actual action to return
                {
                    case "1": pa = new PlayerAction(Name, "Bet1", "bet", amount); break;
                    case "2": pa = new PlayerAction(Name, "Bet1", "raise", amount); break;
                    case "3": pa = new PlayerAction(Name, "Bet1", "call", amount); break;
                    case "4": pa = new PlayerAction(Name, "Bet1", "check", amount); break;
                    case "5": pa = new PlayerAction(Name, "Bet1", "fold", amount); break;
                    default: Console.WriteLine("Invalid menu selection - try again"); continue;
                }
            } while (actionSelection != "1" && actionSelection != "2" &&
                    actionSelection != "3" && actionSelection != "4" &&
                    actionSelection != "5");

            return pa;
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand) // I ended up reusing a lot of the logic in BET1, so I just added the BET2 to that and call it here
        {
            PlayerAction pa1 = BettingRound1(actions, hand);

            return new PlayerAction(pa1.Name, "Bet2", pa1.ActionName, pa1.Amount);
        }

        public override PlayerAction Draw(Card[] hand)
        {
            timesRaisedBet1 = 0; // Reset the betting variables after the first round of betting is over
            timesRaisedBet2 = 0;
            alreadyCountedBluff = false;
            bluffing = false; // We don't bluff in betting round 2, so reset that

            ListTheHand(hand);

            List<int> deleteIndexes = new List<int>();

            int rank = Evaluate.RateAHand(hand, out Card highCard);

            int cardsToDelete = 0;
            do
            {
                string deleteStr = "0";
                Dictionary<int, int> handMap = new Dictionary<int, int>();
                int v = 0;

                switch (rank)
                {
                    case 2: // One pair
                        for (int i = 0; i < hand.Length; i++) // Throw out the 3 non pair cards
                        {
                            if (handMap.ContainsKey(hand[i].Value)) handMap[hand[i].Value]++;
                            else handMap.Add(hand[i].Value, 1);
                        }

                        v = handMap.FirstOrDefault(x => x.Value == 2).Key;

                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value != v) deleteIndexes.Add(i);
                        }

                        deleteStr = "3";
                        break;

                    case 3: // Two pair
                        for (int i = 0; i < hand.Length; i++)// Throw out a single card not in either pair unless it's high
                        {
                            if (handMap.ContainsKey(hand[i].Value)) handMap[hand[i].Value]++;
                            else handMap.Add(hand[i].Value, 1);
                        }

                        v = handMap.FirstOrDefault(x => x.Value == 1).Key;
                        if (v < 14) // If less than ace
                        {
                            deleteStr = "1";
                            for (int i = 0; i < hand.Length; i++)
                            {
                                if (hand[i].Value == v) deleteIndexes.Add(i);
                            }
                        }
                        else deleteStr = "0";
                        break;

                    case 4: // Three of a kind
                        for (int i = 0; i < hand.Length; i++) // Throw out 2 cards that aren't in the 3 of a kind
                        {
                            if (handMap.ContainsKey(hand[i].Value)) handMap[hand[i].Value]++;
                            else handMap.Add(hand[i].Value, 1);
                        }

                        v = handMap.FirstOrDefault(x => x.Value == 3).Key;
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value != v) deleteIndexes.Add(i);
                        }

                        deleteStr = "2";
                        break;

                    case 8: // Four of a kind
                        for (int i = 0; i < hand.Length; i++) // Discard one unless we have a high card
                        {
                            if (handMap.ContainsKey(hand[i].Value)) handMap[hand[i].Value]++;
                            else handMap.Add(hand[i].Value, 1);
                        }

                        v = handMap.FirstOrDefault(x => x.Value == 1).Key;
                        if (v < 14) // Less than Ace
                        {
                            deleteStr = "1";
                            for (int i = 0; i < hand.Length; i++)
                            {
                                if (hand[i].Value == v) deleteIndexes.Add(i);
                            }
                        }
                        else deleteStr = "0";
                        break;

                    case 5:
                    case 6:
                    case 7:
                    case 9:
                    case 10: // Straight, flush, full house, straight flush, or royal flush
                        deleteStr = "0"; // Don't throw cards on full hand sets
                        break;

                    case 1:
                    case 0: // We made it here with nothing, dump 4 cards and start bluffing
                        if (bluffCounter > timeToBluff && disableBluffing == false)
                        {
                            deleteStr = "0";
                            bluffing = true;
                            bluffCounter = 0;
                        }
                        else
                        {
                            deleteStr = "4";
                            for (int i = 0; i < 4; i++)
                            {
                                deleteIndexes.Add(i);
                            }
                        }
                        break;
                }
                int.TryParse(deleteStr, out cardsToDelete);

            } while (cardsToDelete < 0 || cardsToDelete > 5);

            PlayerAction pa = null;
            if (cardsToDelete > 0 && cardsToDelete < 5) // Parse card deletion
            {
                for (int i = 0; i < cardsToDelete; i++) // loop to delete cards
                {
                    Console.WriteLine("\nDelete card " + (i + 1) + ":");
                    for (int j = 0; j < hand.Length; j++)
                    {
                        Console.WriteLine("{0} - {1}", (j + 1), hand[j]);
                    }

                    int delete = 0;
                    do
                    {
                        //Console.Write("Which card to delete? (1 - 5): ");

                        delete = deleteIndexes[i] + 1;

                        if (delete < 1 || delete > 5) delete = 0; // Invalid index
                        else if (hand[delete - 1] == null) delete = 0; // Already deleted
                        else
                        {
                            hand[delete - 1] = null; // Delete entry
                            delete = 99; // Exit loop
                        }
                    } while (delete == 0);
                }

                pa = new PlayerAction(Name, "draw", "draw", cardsToDelete);
            }
            else if (cardsToDelete == 5)
            {
                for (int i = 0; i < hand.Length; i++) // Dump (ideally we folded before this)
                {
                    hand[i] = null;
                }
                pa = new PlayerAction(Name, "draw", "draw", 5);
            }
            else // No discards
            {
                pa = new PlayerAction(Name, "draw", "stand pat", 0);
            }

            return pa;
        }

        private void ListTheHand(Card[] hand) // Helper, evaluates and lists out the hand
        {
            int rank = Evaluate.RateAHand(hand, out Card highCard); // Evaluate the hand

            Console.WriteLine("\nName: " + Name + " Your hand:   Rank: " + rank);
            for (int i = 0; i < hand.Length; i++)
            {
                Console.Write(hand[i].ToString() + " ");
            }
            Console.WriteLine();
        }

        private string Bet1Start(int rank, Card[] hand) // This method handles the decisions if we go first in BET1
        {
            if (rank <= 1) return "4"; // Just check if we don't have much
            else if (rank == 2) // Limp in if we have a pair of aces, baiting them into raising even if they just have a pair
            {
                Dictionary<int, int> handMap = new Dictionary<int, int>();
                for (int i = 0; i < hand.Length; i++)
                {
                    if (handMap.ContainsKey(hand[i].Value)) handMap[hand[i].Value]++; // Loop through the hand and grab a map of how many we have of each card
                    else handMap.Add(hand[i].Value, 1);
                }

                int v = handMap.FirstOrDefault(x => x.Value == 2).Key;
                if (v == 14) return "4"; // Check if we have an ace pair
                else
                {
                    betAmtText = "10";
                    return "1";
                }
            }
            else // If we have better than a pair, up the bet by a small amount; don't go too high or they'll fold and we won't get their money :(
            {
                betAmtText = "20";
                return "1";
            }
        }

        private string Bet1(int rank, Card[] hand, PlayerAction lastAction) // Standard BET1
        {
            betAmtText = "10";
            if (lastAction.ActionName.Equals("check")) // If the opponent last checked
            {
                if (rank <= 1) return "4"; // We should also check, out hand's not great
                else return "1"; // Raise in if we have something decent
            }
            else if (lastAction.ActionName.Equals("bet")) // If opponent bet last
            {
                if (rank <= 1) return "5"; // We have nothing, just fold
                else // Always raise first, never call or we run the risk of losing more on average to the opponent reading us
                {
                    if (timesRaisedBet1 < 1)
                    {
                        timesRaisedBet1++;
                        return "2";
                    }
                    else return "3";
                }
            }
            else if (lastAction.ActionName.Equals("raise")) return "3"; // If they raised us we already bet so ignore rechecks
            else if (lastAction.ActionName.Equals("call")) return "3"; // Should never get here
            else return "4"; // Should really never get here, checking implies we aren't confident
        }

        private string Bet2Start(int rank, Card[] hand, PlayerAction lastAction) // Going first in BET2
        {
            betAmtText = "10";
            if (bluffing) return "1"; // Bluff time
            else if (rank <= 1) return "4"; // Bad hand, check and move on
            else if (lastAction.ActionName.Equals("stand pat")) return "4"; // Should probably fold, would like to be able to tell here if they are bluffing
            else // Find out their discard amount
            {
                int cardsTossed = lastAction.Amount;
                if (cardsTossed >= 4) return "1"; // They had a bad hand and probably didn't get anything better, bet
                if (cardsTossed == 3) // They had a pair
                {
                    if (rank > 2) // If we have a pair or better, take them up on it at first
                    {
                        if (rank >= 4) betAmtText = "30"; // If we have a lot better than what they're likely to get from a draw, smash that bet button
                        return "1";
                    }
                    else return "4"; // Otherwise, check
                }
                if (cardsTossed == 2) // They had three of a kind
                {
                    if (rank > 4) // If we have same or better, bet
                    {
                        if (rank >= 5) betAmtText = "30";  // Smash the bet button if we have a lot better
                        return "1";
                    }
                    else return "4"; // Otherwise, check
                }
                if (cardsTossed == 1) // They had two pair or four of a kind
                {
                    if (rank > 3) // If we have same or better, bet
                    {
                        if (rank >= 4) betAmtText = "30"; // You guessed it, SMASH THAT BET BUTTON
                        return "1";
                    }
                    else return "4";
                }

                return "4"; // Should never get here, but just check if we do
            }
        }

        private string Bet2(int rank, Card[] hand, PlayerAction lastAction, List<PlayerAction> actions) // Standard BET2
        {
            PlayerAction drawAction = actions[actions.Count - 3];  // Check if 3 turns ago was draw and update opponent discards
            if (drawAction.ActionPhase.ToLower().Equals("draw"))
            {
                if (drawAction.ActionName.Equals("stand pat")) opponentDiscards = 0;
                else opponentDiscards = drawAction.Amount;
            }

            betAmtText = "10";
            if (lastAction.ActionName.Equals("check"))
            {
                if (bluffing) return "1"; // Bluff away
                else if (rank <= 1) return "4"; // Not great, let's just check and compare
                else
                {
                    if (opponentDiscards == 0) return "4"; // If they have something good we don't want to lose any more money, just check
                    else if (opponentDiscards == 1) // They had two pair or four of a kind
                    {
                        if (rank > 3) // If we have same or better then bet
                        {
                            if (rank >= 4) betAmtText = "30"; // If we more better we more bet
                            return "1";
                        }
                        else return "4"; // Otherwise, check
                    }
                    else if (opponentDiscards == 2) // They had three of a kind
                    {
                        if (rank >= 4) // Same or better then bet away
                        {
                            if (rank >= 5) betAmtText = "30"; // More bet for more better
                            return "1";
                        }
                        else return "4";
                    }
                    else if (opponentDiscards == 3) // They just had a pair
                    {
                        if (rank >= 2) // Wanna bet
                        {
                            if (rank >= 4) betAmtText = "30"; // Oh yeah? B E T
                            return "1";
                        }
                        else return "4";
                    }
                    else return "1"; // They threw away four cards
                }
            }
            else if (lastAction.ActionName.Equals("bet"))
            {
                if (bluffing) // If they called our bluff
                {
                    disableBluffing = true;
                    return "5";
                }
                else if (rank <= 1) return "5"; // We have nothing, don't keep going
                if (opponentDiscards == 0) // They have a good hand
                {
                    if (rank == 5 || rank == 6) return "3"; // We call with straight or flush
                    else if (rank >= 7) // Full house or better we raise
                    {
                        if (timesRaisedBet2 < 2)
                        {
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3"; // Don't raise more than twice
                    }
                    else return "5"; // We can't fight that, fold
                }
                else if (opponentDiscards == 1) // They had two pair or four of a kind
                {
                    if (rank == 3) return "3";
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 4) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3"; // Don't raise more than once
                    }
                    else return "5"; // Fold if we can't compete
                }
                else if (opponentDiscards == 2) // They had three of a kind
                {
                    if (rank == 4) return "3";
                    else if (rank >= 5)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 5) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3"; // Don't raise more than once
                    }
                    else return "5";
                }
                else if (opponentDiscards == 3) // They had a pair
                {
                    if (rank == 2) return "3";
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 4) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3"; // Don't raise more than once
                    }
                    else return "5";
                }
                else // They had nothing
                {
                    if (rank == 2) return "3";
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 3) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "3";
                }
            }
            else if (lastAction.ActionName.Equals("raise"))
            {
                if (bluffing) // They raised us on a stand pat bluff, don't let them exploit that
                {
                    disableBluffing = true;
                    return "5";
                }
                else if (rank <= 1) return "5";// F O L D
                else if (opponentDiscards == 0) // They have a good hand
                {
                    if (rank == 5 || rank == 6) return "3"; // Straight or flush we call
                    else if (rank >= 7) // Full house or better we raise
                    {
                        if (timesRaisedBet2 < 2)
                        {
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "5"; // If you're reading downward, a lot of this is the same logic slightly altered so comments are the same
                }
                else if (opponentDiscards == 1) // They had two pair or four of a kind
                {
                    if (rank == 3) return "3";
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 4) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "5";
                }
                else if (opponentDiscards == 2) // Three of a kind
                {
                    if (rank == 4) return "3";
                    else if (rank >= 5)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 5) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "5";
                }
                else if (opponentDiscards == 3) // They just had a pair
                {
                    if (rank == 2 || rank == 3) return "3";
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 4) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "5";
                }
                else // They had nothing
                {
                    if (rank == 2) return "3";
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            if (rank >= 3) betAmtText = "30";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else return "3";
                    }
                    else return "3";
                }
            }
            else if (lastAction.ActionName.Equals("call")) return "3"; // If they called, we already analyzed and bet

            else return "4"; // We should never get here, but check
        }
    }
}
