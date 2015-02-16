from IComparer import *

class DefaultComparer(IComparer):
    """ A comparer that compares two objects directly based on their comparison operators. """

    def __init__(self):
        """ Creates a new instance of a default comparer. """
        pass

    def compare(self, Item, Other):
        """ Compares two items and returns an integer describing their relationship to each other. """
        # Pre:
        # 'Item' is the left operand of the comparison, 'Other' is the right operand.
        # Post:
        # Returns 0 if 'Item' and 'Other' are equal, -1 if 'Item' is less than 'Other' and 1 if 'Item' is greater than 'Other'.
        if Item < Other:
            return -1
        elif Item > Other:
            return 1
        else:
            return 0