class IComparer:
    """ Describes a generic comparer: a class containing pure function that compares two objects of the same types, and returns an integer value describing their relationship to each other. """

    def compare(self, Item, Other):
        """ Compares two items and returns an integer describing their relationship to each other. """
        # Pre:
        # 'Item' is the left operand of the comparison, 'Other' is the right operand.
        # Post:
        # Returns 0 if 'Item' and 'Other' are equal, -1 if 'Item' is less than 'Other' and 1 if 'Item' is greater than 'Other'.
        raise NotImplementedError("Method 'IComparer.compare' was not implemented.")