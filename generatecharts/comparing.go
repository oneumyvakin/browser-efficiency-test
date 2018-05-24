package main

import (
	"encoding/json"
	"fmt"
	"github.com/wcharczuk/go-chart"
	"image"
	"image/draw"
	"image/png"
	"io"
	"io/ioutil"
	"os"
	"path/filepath"
)

func mergePng(inputPath1, inputPath2, outPath string) error {
	inFiles1, err := ioutil.ReadDir(inputPath1)
	if err != nil {
		return fmt.Errorf("failed to ReadDir %s err %s", inputPath1, err)
	}
	inFiles2, err := ioutil.ReadDir(inputPath2)
	if err != nil {
		return fmt.Errorf("failed to ReadDir %s err %s", inputPath2, err)
	}
	if _, err := os.Stat(outPath); os.IsNotExist(err) {
		err = os.Mkdir(outPath, 0666)
		if err != nil {
			return fmt.Errorf("failed to Mkdir %s err %s", outPath, err)
		}
	}

	for _, inF1 := range inFiles1 {
		if filepath.Ext(inF1.Name()) != ".png" {
			continue
		}
		for _, inF2 := range inFiles2 {
			//fmt.Println(f.Name(), filepath.Ext(f.Name()))
			if inF1.Name() != inF2.Name() {
				continue
			}

			imgFile1, err := os.Open(filepath.Join(inputPath1, inF1.Name()))
			if err != nil {
				return err
			}
			imgFile2, err := os.Open(filepath.Join(inputPath2, inF2.Name()))
			if err != nil {
				return err
			}

			img1, _, err := image.Decode(imgFile1)
			if err != nil {
				return err
			}
			img2, _, err := image.Decode(imgFile2)
			if err != nil {
				return err
			}

			// starting position of the second image (bottom left)
			sp2 := image.Point{img1.Bounds().Dx(), 0}

			// new rectangle for the second image
			r2 := image.Rectangle{sp2, sp2.Add(img2.Bounds().Size())}

			// rectangle for the big image
			r := image.Rectangle{image.Point{0, 0}, r2.Max}

			// create new image
			rgba := image.NewRGBA(r)

			// draw the two images into this new image
			draw.Draw(rgba, img1.Bounds(), img1, image.Point{0, 0}, draw.Src)
			draw.Draw(rgba, r2, img2, image.Point{0, 0}, draw.Src)

			// save result
			out, err := os.Create(filepath.Join(outPath, inF2.Name()))
			if err != nil {
				return err
			}

			err = png.Encode(out, rgba)
			if err != nil {
				return err
			}
		}
	}
	return nil
}

func serializeChartBars(measureSet string, chartBars []chart.Value) error {
	jsonFilePath := filepath.Join(*pngPath, measureSet+".json")
	jsonFile, err := os.Create(jsonFilePath)
	if err != nil {
		fmt.Printf("Create JSON file %s err %v\n", jsonFilePath, err)
		return fmt.Errorf("Create JSON file %s err %v\n", jsonFilePath, err)
	}

	err = json.NewEncoder(jsonFile).Encode(chartBars)
	if err != nil {
		return fmt.Errorf("Failed to encode JSON to file %s err %v\n", jsonFilePath, err)
	}
	return ioClose(jsonFilePath, jsonFile)
}

func ioClose(description string, c io.Closer) error {
	if err := c.Close(); err != nil {
		fmt.Printf("Failed to close %s: %v\n", description, err)
		return err
	}
	return nil
}
