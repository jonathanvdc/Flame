from DefaultComparer import *
from IComparer import *

class RecordKeyComparer(IComparer):
    """ A comparer that compares records based on their keys. """

    def __init__(self):
        """ Creates a new record key comparer. """
        self.actual_comparer = DefaultComparer()

    def compare(self, Item, Other):
        """ Compares two items and returns an integer describing their relationship to each other. """
        # Pre:
        # 'Item' is the left operand of the comparison, 'Other' is the right operand.
        # Post:
        # Returns 0 if 'Item' and 'Other' are equal, -1 if 'Item' is less than 'Other' and 1 if 'Item' is greater than 'Other'.
        return self.actual_comparer.compare(Item.key, Other.key)