from ArrayList import *
from IListSorter import *

class SortedListSort(IListSorter):
    """ An implementation 'IListSorter<T>' that uses a sorted list to sort a regular list.
        This amounts to treesort when a 'TreeSortedList<T>' is used. """

    def __init__(self, SortedList):
        """ Creates a new instance of a quicksort implementation for 'IListSorter<T>' based on the given sorted list. """
        self.sorted_list = SortedList

    def clear_list(self):
        """ Removes all items from the sorted list.
            This method is private. """
        contents = ArrayList()
        for item in self.sorted_list:
            contents.add(item)
        for item in contents:
            self.sorted_list.remove(item)

    def sort(self, List):
        """ Sorts the items in the given list.
            Whether the results are stored in-place or a new list is created is an implementation detail. """
        # Pre:
        # 'Items' must be a mutable list of items of type 'T'.
        # Post:
        # Returns a list that contains all elements of 'Items' in sorted order. 'Items' may or may not become sorted, but will contain the same elements as before.
        self.clear_list()
        while List.count > 0:
            self.sorted_list.add(List[0])
            List.remove_at(0)
        for input in self.sorted_list:
            List.add(input)
        return List