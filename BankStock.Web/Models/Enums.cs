namespace BankStock.Web.Models;

public enum BankGroup { Big4 = 1, PrivateJSC = 2 }
public enum OrderType { Buy = 1, Sell = 2 }
public enum OrderMode { Market = 1, Limit = 2 }
public enum OrderStatus { Pending = 1, Matched = 2, Cancelled = 3 }
public enum WalletTransactionType { Deposit = 1, Withdraw = 2, InitialGrant = 3, BuyDebit = 4, SellCredit = 5, Fee = 6, Refund = 7 }
