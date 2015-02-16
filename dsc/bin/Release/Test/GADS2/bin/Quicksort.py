from IListSorter import *

class Quicksort(IListSorter):
    """ Defines a quicksort implementation for IListSorter<T>.
        Elements are sorted in ascending order using quicksort based on input from an IComparer<T> which is obtained via a constructor parameter. """

    def __init__(self, ItemComparer):
        """ Creates a new instance of a quicksort implementation for IListSorter<T> based on the given item comparer. """
        self.item_comparer = ItemComparer

    def sort(self, Items):
        """ Sorts the items in the given list.
            Whether the results are stored in-place or a new list is created is an implementation detail. """
        # Pre:
        # 'Items' must be a mutable list of items of type 'T'.
        # Post:
        # Returns a list that contains all elements of 'Items' in sorted order. 'Items' may or may not become sorted, but will contain the same elements as before.
        self.quicksort(Items, 0, Items.count - 1)
        return Items

    def quicksort(self, Items, Start, End):
        """ Sorts (a portion of) the given list list in-place using the quicksort algorithm.
            This method is private. """
        if Start < End:
            p = self.partition(Items, Start, End)
            self.quicksort(Items, Start, p - 1)
            self.quicksort(Items, p + 1, End)

    def partition(self, Items, Start, End):
        """ 'Partitions' the given list.
            Basically, a pivot item is selected, and all items that have a search key less than the pivot will be moved to the start of the list.
            The pivot item will be the next item in the list, followed immediately by all items that have a search key greater than or equal to the pivot.
            The returns value consists of the index of the pivot in the modified list.
            This method is used by the quicksort sorting method.
            This method is private. """
        # Post:
        # Returns the index of the spot in the list where the pivot has been placed.
        pivotIndex = (Start + End) // 2
        pivot = Items[pivotIndex]
        lowIndex = Start
        self.swap(Items, pivotIndex, End)
        i = Start
        while i < End:
            if self.item_comparer.compare(Items[i], pivot) < 0:
                self.swap(Items, lowIndex, i)
                lowIndex += 1
            i += 1
        self.swap(Items, lowIndex, End)
        return lowIndex

    def swap(self, Items, First, Second):
        """ Swaps two items' positions in the list.
            This method is private. """
        temp = Items[First]
        Items[First] = Items[Second]
        Items[Second] = temp

    @property
    def item_comparer(self):
        """ Gets the item comparer that is used to order items. """
        return self.item_comparer_value

    @item_comparer.setter
    def item_comparer(self, value):
        """ Sets the item comparer that is used to order items.
            This accessor is private. """
        self.item_comparer_value = value