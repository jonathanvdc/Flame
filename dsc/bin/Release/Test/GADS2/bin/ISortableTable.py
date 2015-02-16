from ITable import *

class ISortableTable(ITable):
    """ Describes a sortable table. """

    def sort(self, Sorter):
        """ Sorts the table's contents in ascending order based on the the provided comparer. """
        # Pre:
        # The table must not be empty for this method to change the table's state.
        # Post:
        # The table's items will be sorted.
        # After sorting, the table's 'ToList()' method must return a list whose items are sorted.
        # If the table's state is modified after 'Sort()' is called, the list produced by 'ToList()' need no longer be sorted.
        raise NotImplementedError("Method 'ISortableTable.sort' was not implemented.")