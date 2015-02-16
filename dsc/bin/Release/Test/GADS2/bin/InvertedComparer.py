from IComparer import *

class InvertedComparer(IComparer):
    """ A comparer inverts the result of another comparer. """

    def __init__(self, Comparer):
        """ Creates a new inverted comparer that uses the given comparer. """
        self.comparer = Comparer

    def compare(self, Item, Other):
        """ Compares two items and returns an integer describing their relationship to each other. """
        # Pre:
        # 'Item' is the left operand of the comparison, 'Other' is the right operand.
        # Post:
        # Returns 0 if 'Item' and 'Other' are equal, -1 if 'Item' is less than 'Other' and 1 if 'Item' is greater than 'Other'.
        return -self.comparer.compare(Item, Other)

    @property
    def comparer(self):
        """ Gets the internal comparer this comparer uses. """
        return self.comparer_value

    @comparer.setter
    def comparer(self, value):
        """ Sets the internal comparer this comparer uses.
            This accessor is private. """
        self.comparer_value = value