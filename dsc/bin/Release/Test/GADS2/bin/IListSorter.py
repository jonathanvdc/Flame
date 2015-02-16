class IListSorter:
    """ Describes a sorting algorithm that is to be applied on lists.
        The way and order in which the items are sorted is left to individual implementations. """
    # Remarks:
    # Implementations may vary wildly, which is why this type is made as flexible as possible.
    # This does not have to mean a loss of control, as the client chooses which implementation of this type is used to sort the list.
    # Any and all implementations of this list should at least clearly specify the criteria based on which the list will be sorted.

    def sort(self, Items):
        """ Sorts the items in the given list.
            Whether the results are stored in-place or a new list is created is an implementation detail. """
        # Pre:
        # 'Items' must be a mutable list of items of type 'T'.
        # Post:
        # Returns a list that contains all elements of 'Items' in sorted order. 'Items' may or may not become sorted, but will contain the same elements as before.
        raise NotImplementedError("Method 'IListSorter.sort' was not implemented.")