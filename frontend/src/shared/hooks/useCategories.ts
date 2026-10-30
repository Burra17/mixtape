import { useEffect, useState } from 'react';
import { getCategories } from '../../services/hub/connection.ts';
import type { CategoryOption } from '../../services/hub/hubContract.ts';
import { useIsConnected } from '../../store/connectionStore.ts';

// Drop the Needles kategorier, som inte ändras medan servern kör. Hämtas när anslutningen finns, och en tom lista betyder att de inte kommit än.
export const useCategories = () => {
  const isConnected = useIsConnected();
  const [categories, setCategories] = useState<CategoryOption[]>([]);

  useEffect(() => {
    if (!isConnected || categories.length > 0) return;
    getCategories()
      .then(setCategories)
      .catch(() => undefined);
  }, [isConnected, categories.length]);

  return categories;
};

// Namnet på kategorin, eller dess id tills kategorierna har hämtats.
export const categoryName = (categories: CategoryOption[], id: string) => categories.find((category) => category.id === id)?.name ?? id;
