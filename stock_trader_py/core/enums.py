import enum

class Exchange(enum.Enum):
    NSE = "NSE"
    BSE = "BSE"

class TipType(enum.Enum):
    INTRADAY = "Intraday"
    OPTIONS = "Options"
    SWING = "Swing"

class ActionType(enum.Enum):
    BUY = "Buy"
    SELL = "Sell"
    HOLD = "Hold"
